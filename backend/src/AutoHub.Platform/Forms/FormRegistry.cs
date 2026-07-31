using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Forms;

/// <summary>
/// Serves published forms and validates payloads against them.
/// </summary>
/// <remarks>
/// Both operations go through the same compiled document deliberately. If validation read the rows
/// while serving read <see cref="FormVersion.SchemaJson"/>, the two would eventually disagree — the
/// classic version of which is a payload the client was told is fine and the server rejects, with no
/// field highlighted and no way for the user to proceed.
/// </remarks>
public sealed class FormRegistry : IFormRegistry
{
    // Matches the Cache-Control the contract advertises. A form is read on nearly every screen and
    // changes a few times a month; a bounded staleness window is the honest trade against a database
    // round trip plus a taxonomy resolve on every request. Publishing takes effect within it, and an
    // operator who cannot wait a minute calls Invalidate.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private static readonly ConcurrentDictionary<string, int> Generations = new(StringComparer.Ordinal);

    private readonly IFormStore _store;
    private readonly FormSchemaCompiler _compiler;
    private readonly FormValidationEngine _validator;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FormRegistry> _logger;

    public FormRegistry(
        IFormStore store,
        FormSchemaCompiler compiler,
        FormValidationEngine validator,
        IMemoryCache cache,
        ILogger<FormRegistry> logger)
    {
        _store = store;
        _compiler = compiler;
        _validator = validator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<CompiledForm>> GetPublishedAsync(
        string key,
        FormContext context,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(key, context);

        if (_cache.TryGetValue(cacheKey, out CompiledForm? cached) && cached is not null)
        {
            return cached;
        }

        var definition = await _store.GetAsync(key, cancellationToken);
        if (definition is null)
        {
            return Error.NotFound("form.not_found", $"No form named '{key}'.").With("formKey", key);
        }

        if (!definition.IsActive)
        {
            // Retired, not deleted — submissions made against it still have to be explainable.
            return Error.NotFound("form.inactive", $"Form '{key}' is no longer in use.").With("formKey", key);
        }

        var version = definition.SelectVariant(context.ToJson());
        if (version is null)
        {
            // Either nothing is published yet, or every published variant has a selector and none of
            // them matched. Both are configuration faults an operator must see, not a 404 the client
            // should retry.
            _logger.LogError(
                "Form {FormKey} has no published version matching context {Context}",
                key, context.CacheDiscriminator());

            return Error.Unexpected(
                "form.not_published",
                $"No published version of form '{key}' applies here.")
                .With("formKey", key);
        }

        var compiled = version.SchemaJson is { } schemaJson
            ? FormJson.Deserialize(schemaJson)
            : FormSchemaCompiler.Project(definition.Key, version);

        if (compiled is null)
        {
            _logger.LogError("Form {FormKey} v{Version} has an unreadable stored schema", key, version.Version);
            return Error.Unexpected("form.schema_unreadable", "This form is misconfigured and cannot be shown.");
        }

        var hydrated = await _compiler.HydrateAsync(compiled, context.Locale, cancellationToken);
        if (hydrated.IsFailure)
        {
            return hydrated;
        }

        _cache.Set(cacheKey, hydrated.Value, CacheDuration);
        return hydrated;
    }

    public async Task<Result> ValidateAsync(
        string key,
        FormContext context,
        JsonObject payload,
        FormValidationMode mode,
        CancellationToken cancellationToken)
    {
        var form = await GetPublishedAsync(key, context, cancellationToken);
        if (form.IsFailure)
        {
            return Result.Failure(form.Error);
        }

        var report = await _validator.ValidateAsync(form.Value, payload, context, mode, cancellationToken);
        return report.ToResult();
    }

    /// <summary>
    /// Drops every cached variant and locale of one form. Called after a publish so an operator sees
    /// their change immediately rather than believing it did not work and publishing again.
    /// </summary>
    /// <remarks>
    /// Implemented by bumping a per-form generation that the cache key includes, rather than by
    /// enumerating and removing entries — <see cref="IMemoryCache"/> cannot enumerate by prefix, and
    /// tracking every key we ever wrote would grow one entry per locale we ever serve. The orphaned
    /// entries fall out on their own expiry, and the generation map is bounded by the number of forms.
    /// </remarks>
    public static void Invalidate(string key)
        => Generations.AddOrUpdate(key, 1, (_, current) => current + 1);

    private static string CacheKey(string key, FormContext context)
    {
        var generation = Generations.GetValueOrDefault(key, 0);
        return $"forms:{key}:{generation}:{context.CacheDiscriminator()}:{context.Locale ?? "-"}";
    }
}
