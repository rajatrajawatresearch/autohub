using System.Text.Json.Nodes;
using AutoHub.Api.Security;
using AutoHub.Platform.Forms;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Api.Application.Platform;

/// <summary>
/// <c>GET /platform/forms/{key}</c>.
/// </summary>
/// <remarks>
/// The locale is not a parameter of the request the client sends — it comes from
/// <c>Accept-Language</c>, resolved here rather than at the endpoint so the same rule applies to
/// every caller of the form registry.
/// </remarks>
internal sealed record GetFormQuery(string Key, string? Context) : IRequest<Result<CompiledForm>>;

internal sealed class GetFormHandler : IRequestHandler<GetFormQuery, Result<CompiledForm>>
{
    private readonly IFormRegistry _registry;
    private readonly IRequestContext _request;

    public GetFormHandler(IFormRegistry registry, IRequestContext request)
    {
        _registry = registry;
        _request = request;
    }

    public Task<Result<CompiledForm>> Handle(GetFormQuery query, CancellationToken cancellationToken)
        => _registry.GetPublishedAsync(
            query.Key,
            FormContext.Parse(query.Context, _request.Locale),
            cancellationToken);
}

/// <summary>
/// <c>POST /platform/forms/{key}/validate</c>.
/// </summary>
/// <remarks>
/// <see cref="Complete"/> distinguishes the two things a client asks for with one endpoint. The sell
/// flow validates step by step and must not be told about fields on step four; the submit button
/// validates everything. Defaulting to partial is the safer direction — a caller who forgets the
/// flag gets fewer errors, not a form that refuses to advance past step one.
/// </remarks>
internal sealed record ValidateFormCommand(string Key, string? Context, JsonObject Payload, bool Complete)
    : IRequest<Result>;

internal sealed class ValidateFormHandler : IRequestHandler<ValidateFormCommand, Result>
{
    private readonly IFormRegistry _registry;
    private readonly IRequestContext _request;

    public ValidateFormHandler(IFormRegistry registry, IRequestContext request)
    {
        _registry = registry;
        _request = request;
    }

    public Task<Result> Handle(ValidateFormCommand command, CancellationToken cancellationToken)
        => _registry.ValidateAsync(
            command.Key,
            FormContext.Parse(command.Context, _request.Locale),
            command.Payload,
            command.Complete ? FormValidationMode.Complete : FormValidationMode.Partial,
            cancellationToken);
}
