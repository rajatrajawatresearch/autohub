using System.Text.Json.Nodes;
using AutoHub.Api.Application.Workflow;
using AutoHub.Api.Http;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// The two generic workflow endpoints, serving every module at once.
/// </summary>
/// <remarks>
/// There is one route here, not one per module, and that is the whole architectural bet made
/// visible: <c>/listings/listings/{id}/workflow</c> and <c>/auction/lots/{id}/workflow</c> are the
/// same code, because a listing's lifecycle and an auction lot's lifecycle are the same mechanism
/// with different data. A new module gets working workflow endpoints by publishing a definition.
/// <para>
/// Mapped last, after every module's own routes. The <c>{module}/{resource}</c> pattern is broad
/// enough to shadow a real route, and route precedence in ASP.NET Core prefers literal segments over
/// parameters — but only when both are registered, so registration order is the belt to that braces.
/// </para>
/// </remarks>
internal static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{module}/{resource}/{id:guid}/workflow", async (
                string module,
                string resource,
                Guid id,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var snapshot = await sender.Send(new GetEntityWorkflowQuery(module, resource, id), ct);

                // Never cached. This is live state plus the actions *this* user may fire right now,
                // and a cached copy is a client showing an Approve button for a listing somebody
                // else approved thirty seconds ago.
                return snapshot.IsSuccess
                    ? Results.Ok(snapshot.Value)
                    : ApiProblem.Result(snapshot.Error, context);
            })
            .WithName("GetEntityWorkflow")
            .WithTags("Workflow")
            .AllowAnonymous()
            .WithSummary("The live state of one entity and the actions this user may fire.");

        app.MapPost("/{module}/{resource}/{id:guid}/workflow/actions", async (
                string module,
                string resource,
                Guid id,
                WorkflowActionBody body,
                ISender sender,
                HttpContext context,
                CancellationToken ct) =>
            {
                var snapshot = await sender.Send(
                    new FireWorkflowActionCommand(module, resource, id, body.ActionCode, body.Reason, body.Payload),
                    ct);

                // The same document the GET returns, so the client replaces its state rather than
                // reconciling it. A 409 carries metadata.currentState, so a stale tab refreshes from
                // the truth instead of guessing.
                return snapshot.IsSuccess
                    ? Results.Ok(snapshot.Value)
                    : ApiProblem.Result(snapshot.Error, context);
            })
            .WithName("FireWorkflowAction")
            .WithTags("Workflow")
            .RequireAuthorization()
            .WithSummary("Fires a transition. Requires an Idempotency-Key.");

        return app;
    }
}

/// <summary>
/// The action request body.
/// </summary>
/// <remarks>
/// <see cref="Payload"/> is untyped because a transition may be gated on a form the workflow
/// registry names — a rejection reason form, a payout approval form — and the shape of that form is
/// registry data. A typed body would mean a new field on a rejection form is a backend change.
/// </remarks>
public sealed record WorkflowActionBody(string ActionCode, string? Reason, JsonObject? Payload);
