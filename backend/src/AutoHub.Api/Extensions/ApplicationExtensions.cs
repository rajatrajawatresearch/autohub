using System.Reflection;
using AutoHub.Api.Application.Behaviours;
using FluentValidation;
using MediatR;

namespace AutoHub.Api.Extensions;

internal static class ApplicationExtensions
{
    /// <summary>
    /// MediatR, the validators, and the two behaviours every request passes through.
    /// </summary>
    /// <remarks>
    /// <b>Behaviour order is the registration order and it is load-bearing.</b> Validation runs
    /// first, so a request that is going to be rejected never opens a transaction — otherwise every
    /// malformed submission costs a connection from the pool and a write-ahead-log entry for a
    /// transaction that will only ever roll back.
    /// <para>
    /// The transaction behaviour is constrained to <see cref="ITransactionalRequest"/>, so the
    /// container simply does not construct it for the registry reads that make up most of the
    /// traffic.
    /// </para>
    /// </remarks>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        builder.Services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehaviour<,>));
        });

        // includeInternalTypes, because every handler and validator in this host is internal — a
        // public type here would be part of an API surface nothing consumes.
        builder.Services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return builder;
    }
}
