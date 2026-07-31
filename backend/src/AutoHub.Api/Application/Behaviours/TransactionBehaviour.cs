using AutoHub.Persistence;
using AutoHub.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AutoHub.Api.Application.Behaviours;

/// <summary>
/// Marks a request as one write that must commit or roll back as a whole.
/// </summary>
/// <remarks>
/// Opt-in rather than applied to everything. A transaction around a read holds a snapshot and a
/// connection for the life of the request and buys nothing, and the registry reads are the busiest
/// endpoints in the product.
/// <para>
/// It is an interface rather than an attribute so the generic constraint on
/// <see cref="TransactionBehaviour{TRequest,TResponse}"/> does the filtering — the behaviour is
/// simply not constructed for a request that does not implement it, instead of being constructed
/// and then deciding to do nothing.
/// </para>
/// </remarks>
public interface ITransactionalRequest;

/// <summary>
/// Wraps a command in one database transaction.
/// </summary>
/// <remarks>
/// The interesting decision is that a <b>failed <see cref="Result"/> rolls back</b>, not just a
/// thrown exception. Without that, a handler that writes an audit row, then discovers the workflow
/// definition is invalid and returns a failure, leaves the audit row behind: a trail asserting a
/// publish that did not happen. Expected failures are the common case in this codebase, so they have
/// to be transactional too.
/// <para>
/// The retrying execution strategy is what makes this correct under Npgsql's
/// <c>EnableRetryOnFailure</c>. Opening a transaction by hand while retries are enabled throws
/// outright, because a retry would replay only part of the unit of work — the strategy has to own
/// the whole block so it can replay all of it.
/// </para>
/// <para>
/// A nested transaction is joined rather than started. The idempotency middleware claims its key in
/// its own transaction before the handler runs, and a handler that opened a second one underneath
/// would deadlock against it.
/// </para>
/// </remarks>
internal sealed class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest
{
    private readonly AutoHubDbContext _context;
    private readonly ILogger<TransactionBehaviour<TRequest, TResponse>> _logger;

    public TransactionBehaviour(
        AutoHubDbContext context,
        ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return await next();
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var response = await next();

            if (IsFailure(response))
            {
                _logger.LogInformation(
                    "{Request} returned a failure; rolling back", typeof(TRequest).Name);

                await transaction.RollbackAsync(cancellationToken);
                return response;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        });
    }

    private static bool IsFailure(TResponse response) => response switch
    {
        Result result => result.IsFailure,
        // Result<T> is a struct with no shared base, so the generic case is answered by the property
        // the two shapes agree on. One reflected property read per failed command is not a cost
        // worth a second type parameter on every command in the codebase.
        not null => response.GetType() is { IsGenericType: true } type
                    && type.GetGenericTypeDefinition() == typeof(Result<>)
                    && (bool)(type.GetProperty(nameof(Result.IsFailure))!.GetValue(response) ?? false),
        _ => false,
    };
}
