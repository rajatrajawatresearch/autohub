namespace AutoHub.Modules.Deals.Domain;

/// <summary>
/// The workflow registry key this module's lifecycle is defined under, and the verbs it fires.
/// </summary>
/// <remarks>
/// <b>Why these constants are not the hardcoding the prime directive bans.</b> The ban is on
/// business meaning living in code: a state string that an <c>if</c> compares against, a percentage,
/// an option list. None of that is here. There is no state code in this file, and there is none
/// anywhere else in the module — which states exist, what they are called, which of them is initial,
/// which are terminal, what an operator must have permission to do and what happens as a
/// consequence is entirely rows in <c>platform.workflow_definitions</c>.
/// <para>
/// What lives here is the <i>name of the command</i>. <see cref="Accept"/> is the identity of the
/// "accept this deal" use case, the same way <c>POST /deals/{id}/workflow/actions</c> with
/// <c>"actionCode": "accept"</c> is its identity on the wire. An operator inserting a legal-hold
/// state between acceptance and settlement changes where <see cref="Settle"/> is available from and
/// what it does — without touching this file, because the code never learns the answer.
/// </para>
/// <para>
/// This is the same boundary <c>WorkflowEffect</c> draws from the other side: which effects run is
/// data, what an effect can do is code. Which transitions an action performs is data; that an action
/// exists to be performed is code.
/// </para>
/// </remarks>
public static class DealWorkflow
{
    /// <summary>Matches <c>WorkflowDefinition.Key</c>.</summary>
    public const string Key = "deal";

    /// <summary>Matches <c>WorkflowDefinition.EntityType</c>, and the outbox's aggregate naming.</summary>
    public const string EntityType = "deals.deal";

    /// <summary>Fired once the pricing engine has produced a quote and it has been recorded.</summary>
    public const string Quote = "quote";

    public const string Accept = "accept";

    public const string Cancel = "cancel";

    public const string Settle = "settle";
}
