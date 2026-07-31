namespace AutoHub.Modules.Payments.Domain.Ledger;

/// <summary>
/// The database's half of the balancing invariant.
/// </summary>
/// <remarks>
/// <b>Why the check exists twice.</b> <see cref="Journal.Post"/> refuses to construct an unbalanced
/// journal, and that protects every write that goes through this codebase. It protects nothing else.
/// A repair script written at 3am, a data migration, a future service holding the same connection
/// string, an ORM that decides to flush a partially built graph — none of them run
/// <see cref="Journal.Post"/>. The constraint below is what makes an unbalanced ledger impossible
/// rather than merely unlikely, and a ledger that is only unlikely to be wrong is a ledger nobody can
/// audit.
/// <para>
/// <b>Why a constraint trigger and not a plain CHECK.</b> A <c>CHECK</c> sees one row. The invariant
/// spans a journal's rows, and those rows do not all exist until the statement that inserts them has
/// finished. A <c>CONSTRAINT TRIGGER … DEFERRABLE INITIALLY DEFERRED</c> is PostgreSQL's mechanism
/// for exactly this: it fires at <c>COMMIT</c>, when the whole journal is present, and it aborts the
/// transaction if the entries do not sum to zero or do not agree with the totals the journal header
/// claims. The row-level <c>CHECK</c>s stay as well — they are cheaper, they fire earlier, and they
/// catch the common mistakes with a better message.
/// </para>
/// <para>
/// Kept here, next to the aggregate whose invariant it is, rather than buried in a migration.
/// Migrations are rewritten every time the model changes; this text is the specification and the
/// migration applies it.
/// </para>
/// </remarks>
public static class LedgerSchema
{
    /// <summary>Schema the ledger tables live in.</summary>
    public const string Schema = "payments";

    /// <summary>
    /// Creates the deferred balancing constraint. Idempotent; safe to run on every migration.
    /// </summary>
    /// <remarks>
    /// The function compares three numbers per journal: the sum of debit entries, the sum of credit
    /// entries, and the header's declared totals. Checking the header too is what stops a correct set
    /// of entries from sitting under a header that reports a different figure to every report that
    /// reads the header instead of the entries — which is most of them, because the header is
    /// indexed.
    /// </remarks>
    public const string CreateBalanceConstraint = """
        CREATE OR REPLACE FUNCTION payments.assert_journal_balances() RETURNS trigger
        LANGUAGE plpgsql AS $$
        DECLARE
            v_journal_id   uuid := COALESCE(NEW.journal_id, OLD.journal_id);
            v_debits       bigint;
            v_credits      bigint;
            v_head_debits  bigint;
            v_head_credits bigint;
        BEGIN
            SELECT
                COALESCE(SUM(amount_minor_units) FILTER (WHERE direction = 1), 0),
                COALESCE(SUM(amount_minor_units) FILTER (WHERE direction = 2), 0)
            INTO v_debits, v_credits
            FROM payments.ledger_entries
            WHERE journal_id = v_journal_id;

            SELECT total_debits_minor_units, total_credits_minor_units
            INTO v_head_debits, v_head_credits
            FROM payments.ledger_journals
            WHERE id = v_journal_id;

            IF v_head_debits IS NULL THEN
                RAISE EXCEPTION 'ledger.journal_missing: entries reference journal % which does not exist', v_journal_id;
            END IF;

            IF v_debits <> v_credits THEN
                RAISE EXCEPTION 'ledger.journal_unbalanced: journal % has debits % and credits %',
                    v_journal_id, v_debits, v_credits;
            END IF;

            IF v_debits <> v_head_debits OR v_credits <> v_head_credits THEN
                RAISE EXCEPTION 'ledger.journal_totals_disagree: journal % entries (%/%) do not match header (%/%)',
                    v_journal_id, v_debits, v_credits, v_head_debits, v_head_credits;
            END IF;

            RETURN NULL;
        END;
        $$;

        DROP TRIGGER IF EXISTS trg_ledger_entries_balance ON payments.ledger_entries;

        CREATE CONSTRAINT TRIGGER trg_ledger_entries_balance
            AFTER INSERT OR UPDATE OR DELETE ON payments.ledger_entries
            DEFERRABLE INITIALLY DEFERRED
            FOR EACH ROW EXECUTE FUNCTION payments.assert_journal_balances();
        """;

    /// <summary>
    /// Revokes the ability to change history at all.
    /// </summary>
    /// <remarks>
    /// Belt and braces over the application's immutability. The trigger above would catch an update
    /// that unbalances a journal; this one catches an update that keeps it balanced while changing
    /// what it says — swapping two accounts, editing a memo, moving an entry to another owner. There
    /// is no legitimate reason for either, and a ledger row that can be edited is a ledger row an
    /// auditor is right to distrust.
    /// </remarks>
    public const string CreateImmutabilityConstraint = """
        CREATE OR REPLACE FUNCTION payments.reject_ledger_mutation() RETURNS trigger
        LANGUAGE plpgsql AS $$
        BEGIN
            RAISE EXCEPTION
                'ledger.entry_immutable: ledger entries are append-only; post a reversing journal instead';
        END;
        $$;

        DROP TRIGGER IF EXISTS trg_ledger_entries_immutable ON payments.ledger_entries;

        CREATE TRIGGER trg_ledger_entries_immutable
            BEFORE UPDATE OR DELETE ON payments.ledger_entries
            FOR EACH ROW EXECUTE FUNCTION payments.reject_ledger_mutation();
        """;

    public const string DropBalanceConstraint = """
        DROP TRIGGER IF EXISTS trg_ledger_entries_balance ON payments.ledger_entries;
        DROP FUNCTION IF EXISTS payments.assert_journal_balances();
        """;

    public const string DropImmutabilityConstraint = """
        DROP TRIGGER IF EXISTS trg_ledger_entries_immutable ON payments.ledger_entries;
        DROP FUNCTION IF EXISTS payments.reject_ledger_mutation();
        """;

    /// <summary>Row-level checks the EF configuration attaches to <c>payments.ledger_journals</c>.</summary>
    public static class JournalChecks
    {
        public const string BalancedName = "ck_ledger_journals_balanced";

        public const string Balanced = "total_debits_minor_units = total_credits_minor_units";

        public const string NonEmptyName = "ck_ledger_journals_non_empty";

        public const string NonEmpty = "total_debits_minor_units > 0";
    }

    /// <summary>Row-level checks the EF configuration attaches to <c>payments.ledger_entries</c>.</summary>
    public static class EntryChecks
    {
        public const string AmountPositiveName = "ck_ledger_entries_amount_positive";

        public const string AmountPositive = "amount_minor_units > 0";

        public const string DirectionName = "ck_ledger_entries_direction";

        public const string Direction = "direction IN (1, 2)";
    }
}
