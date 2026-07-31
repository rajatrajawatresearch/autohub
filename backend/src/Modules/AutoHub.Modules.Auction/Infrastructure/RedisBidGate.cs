using System.Globalization;
using AutoHub.Modules.Auction.Domain;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AutoHub.Modules.Auction.Infrastructure;

/// <summary>
/// The bid gate, implemented as one Lua script executed inside Redis.
/// </summary>
/// <remarks>
/// See <see cref="IBidGate"/> for why the critical section cannot live in C# and why an unavailable
/// Redis rejects rather than accepts. This class is the mechanism; that interface is the argument.
/// </remarks>
public sealed class RedisBidGate : IBidGate
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisBidGate> _logger;

    public RedisBidGate(IConnectionMultiplexer redis, ILogger<RedisBidGate> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Namespace for every key this gate writes.
    /// </summary>
    /// <remarks>
    /// A technical constant. The hash tag around the lot id is load-bearing rather than decorative:
    /// it pins the lot's state key and its idempotency keys to the same Redis Cluster slot, and a Lua
    /// script may only touch keys in one slot. Without it the script works on a single node and fails
    /// the day the cache is clustered, which is the day the auction is busy enough to need clustering.
    /// </remarks>
    private const string KeyPrefix = "autohub:auction:lot:{";

    /// <summary>
    /// How long a decided idempotency key is remembered, in seconds.
    /// </summary>
    /// <remarks>
    /// Long enough to outlive any retry a client will make — mobile clients retry for minutes on a
    /// flaky connection — and short enough that a long-running auction's keys do not accumulate
    /// without bound. It is a technical parameter of the retry window, not a business rule.
    /// </remarks>
    private const int IdempotencyTtlSeconds = 900;

    /// <summary>
    /// The atomic admission decision.
    /// </summary>
    /// <remarks>
    /// <b>Everything below runs to completion with no other bid interleaved.</b> Redis is
    /// single-threaded for command execution and a script is one command, so between the first
    /// <c>HGET</c> and the final <c>HSET</c> no other dealer's script can observe or modify the lot.
    /// That property is the entire reason this logic is here and not in the handler.
    /// <para>
    /// <b>The steps, in order, and why each is inside the section rather than outside it.</b>
    /// </para>
    /// <list type="number">
    /// <item><b>Idempotency replay.</b> Checked first so a retry costs one lookup and cannot
    /// re-enter the decision. Outside the section, a retry racing its own original would be decided
    /// twice.</item>
    /// <item><b>Existence.</b> A missing hash means the gate's state was lost, not that the lot is
    /// new. Returning <c>closed</c> is the safe direction; assuming a fresh lot would reopen bidding
    /// at the start price on a lot already standing at ₹8 lakh.</item>
    /// <item><b>Open and not past close.</b> Compared against the server clock passed in, so every
    /// node agrees. A client-supplied time would let a modified client bid after the hammer.</item>
    /// <item><b>Minimum.</b> The first bid must meet the start price; every later bid must clear the
    /// standing price plus the registry's increment for that price band. Computed inside because the
    /// standing price it depends on can change between two bids a microsecond apart.</item>
    /// <item><b>Proxy resolution.</b> The bidder's ceiling is compared against the standing bidder's
    /// ceiling, and the price moves to just enough to beat the loser — capped at the winner's
    /// ceiling. Equal ceilings leave the incumbent in place, which is the tie-break by server receive
    /// order: they got here first, and "first" is defined by which script ran first inside
    /// Redis.</item>
    /// <item><b>Sequence.</b> Incremented under the same lock, so two bids in the same millisecond
    /// get a total order that neither client's clock can dispute.</item>
    /// <item><b>Anti-snipe.</b> Evaluated after acceptance, using the same <c>now</c> the decision
    /// used. Evaluated outside, a bid could be accepted against a close that had already moved, or
    /// extend a lot whose bid was then rejected.</item>
    /// <item><b>Record the outcome under the idempotency key.</b> Last, so a retry replays exactly
    /// what the original returned rather than a partially applied version of it.</item>
    /// </list>
    /// <para>
    /// The script returns a flat array rather than a map because Redis's Lua-to-RESP conversion turns
    /// tables into arrays and silently drops anything after the first <c>nil</c>. Positional fields
    /// with no nils are the only shape that survives the conversion intact.
    /// </para>
    /// </remarks>
    private const string AdmitScript = """
        -- KEYS[1] lot state hash        KEYS[2] idempotency key for this attempt
        -- ARGV[1] bidder id             ARGV[2] amount offered (minor units)
        -- ARGV[3] proxy ceiling         ARGV[4] minimum increment (minor units)
        -- ARGV[5] now (unix ms)         ARGV[6] idempotency ttl (seconds)

        -- Every reply is eight positional fields:
        -- outcome, standing, bidder, seq, closes_at, extensions, extended, replay
        local prior = redis.call('GET', KEYS[2])
        if prior then
          local f = {}
          for token in string.gmatch(prior, '([^|]+)') do f[#f + 1] = token end
          return { f[1], f[2], f[3], f[4], f[5], f[6], '0', '1' }
        end

        if redis.call('EXISTS', KEYS[1]) == 0 then
          -- No state. Not a new lot: a new lot is seeded by OpenAsync. Refuse.
          return { 'closed', '0', '', '0', '0', '0', '0', '0' }
        end

        local state      = redis.call('HMGET', KEYS[1],
                             'open', 'closes_at', 'high_amount', 'high_bidder',
                             'high_max', 'seq', 'extensions', 'start_price',
                             'snipe_window', 'snipe_extension', 'max_extensions')
        local open       = tonumber(state[1]) or 0
        local closes_at  = tonumber(state[2]) or 0
        local high       = tonumber(state[3]) or 0
        local holder     = state[4] or ''
        local high_max   = tonumber(state[5]) or 0
        local seq        = tonumber(state[6]) or 0
        local extensions = tonumber(state[7]) or 0
        local start      = tonumber(state[8]) or 0
        local window     = tonumber(state[9]) or 0
        local extend_by  = tonumber(state[10]) or 0
        local max_ext    = tonumber(state[11]) or 0

        local bidder     = ARGV[1]
        local offered    = tonumber(ARGV[2])
        local ceiling    = tonumber(ARGV[3])
        local increment  = tonumber(ARGV[4])
        local now        = tonumber(ARGV[5])
        local ttl        = tonumber(ARGV[6])

        if open == 0 or now > closes_at then
          return { 'closed', tostring(high), holder, tostring(seq), tostring(closes_at),
                   tostring(extensions), '0', '0' }
        end

        -- A ceiling below the offer is a client bug; the offer always binds.
        if ceiling < offered then ceiling = offered end

        local required = start
        if high > 0 then required = high + increment end

        if offered < required then
          return { 'too_low', tostring(high), holder, tostring(seq), tostring(closes_at),
                   tostring(extensions), '0', '0' }
        end

        local outcome
        if holder == '' then
          -- First bid. It stands at the floor, not at the ceiling: a proxy bidder pays the least
          -- they need to, which is what makes leaving a ceiling safe enough to be worth doing.
          high     = required
          holder   = bidder
          high_max = ceiling
          outcome  = 'accepted'
        elseif holder == bidder then
          -- Raising your own ceiling. The price does not move; bidding against yourself would be
          -- the platform taking money from a dealer for nothing.
          if ceiling <= high_max then
            return { 'too_low', tostring(high), holder, tostring(seq), tostring(closes_at),
                     tostring(extensions), '0', '0' }
          end
          high_max = ceiling
          outcome  = 'accepted'
        elseif ceiling > high_max then
          -- The challenger's ceiling beats the incumbent's. Price rises to just past the incumbent,
          -- capped at what the challenger authorised.
          local price = high_max + increment
          if price > ceiling then price = ceiling end
          high     = price
          holder   = bidder
          high_max = ceiling
          outcome  = 'accepted'
        else
          -- The incumbent's ceiling holds, including on an exact tie: whoever's script ran first
          -- keeps the lot. Their price rises to just past the challenger, capped at their ceiling.
          local price = ceiling + increment
          if price > high_max then price = high_max end
          if price > high then high = price end
          outcome = 'outbid'
        end

        seq = seq + 1

        local extended = 0
        if window > 0 and max_ext > 0 and (closes_at - now) <= (window * 1000) and extensions < max_ext then
          closes_at  = now + (extend_by * 1000)
          extensions = extensions + 1
          extended   = 1
        end

        redis.call('HMSET', KEYS[1],
          'high_amount', tostring(high),
          'high_bidder', holder,
          'high_max',    tostring(high_max),
          'seq',         tostring(seq),
          'closes_at',   tostring(closes_at),
          'extensions',  tostring(extensions))

        local result = { outcome, tostring(high), holder, tostring(seq), tostring(closes_at),
                         tostring(extensions), tostring(extended), '0' }

        redis.call('SET', KEYS[2],
          table.concat({ outcome, tostring(high), holder, tostring(seq), tostring(closes_at),
                         tostring(extensions) }, '|'),
          'EX', ttl)

        return result
        """;

    public async Task<Result> OpenAsync(BidGateState state, CancellationToken cancellationToken)
    {
        try
        {
            var database = _redis.GetDatabase();

            await database.HashSetAsync(StateKey(state.LotId),
            [
                new HashEntry("open", 1),
                new HashEntry("closes_at", state.ClosesAt.ToUnixTimeMilliseconds()),
                new HashEntry("start_price", state.StartPrice.MinorUnits),
                new HashEntry("high_amount", state.StandingAmount?.MinorUnits ?? 0),
                new HashEntry("high_bidder", state.StandingBidderId?.ToString("N", CultureInfo.InvariantCulture) ?? string.Empty),
                new HashEntry("high_max", state.StandingMaximum?.MinorUnits ?? state.StandingAmount?.MinorUnits ?? 0),
                new HashEntry("seq", state.Sequence),
                new HashEntry("extensions", state.ExtensionCount),
                new HashEntry("snipe_window", state.AntiSnipeWindowSeconds),
                new HashEntry("snipe_extension", state.AntiSnipeExtensionSeconds),
                new HashEntry("max_extensions", state.MaxExtensions),
            ]);

            return Result.Success();
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Could not seed the bid gate for lot {LotId}", state.LotId);
            return Unavailable();
        }
    }

    public async Task<Result<BidAdmission>> AdmitAsync(BidAttempt attempt, CancellationToken cancellationToken)
    {
        RedisResult raw;

        try
        {
            var database = _redis.GetDatabase();

            raw = await database.ScriptEvaluateAsync(
                AdmitScript,
                [StateKey(attempt.LotId), IdempotencyKey(attempt.LotId, attempt.IdempotencyKey)],
                [
                    attempt.BidderId.ToString("N", CultureInfo.InvariantCulture),
                    attempt.Amount.MinorUnits,
                    attempt.MaximumAmount?.MinorUnits ?? attempt.Amount.MinorUnits,
                    attempt.MinimumIncrement.MinorUnits,
                    attempt.PlacedAt.ToUnixTimeMilliseconds(),
                    IdempotencyTtlSeconds,
                ]);
        }
        catch (RedisException ex)
        {
            // The whole reason this method returns a Result. The bid is refused, loudly, and the
            // dealer is told to try again — never accepted on the assumption that it would probably
            // have won. See IBidGate for why that asymmetry is not a judgement call.
            _logger.LogError(ex, "Bid gate unavailable for lot {LotId}; bid refused", attempt.LotId);
            return Unavailable();
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Bid gate timed out for lot {LotId}; bid refused", attempt.LotId);
            return Unavailable();
        }

        return Interpret(raw, attempt);
    }

    public async Task<Result<BidGateSnapshot>> PeekAsync(Guid lotId, CancellationToken cancellationToken)
    {
        try
        {
            var database = _redis.GetDatabase();

            var fields = await database.HashGetAsync(StateKey(lotId),
                ["high_amount", "high_bidder", "seq", "closes_at", "extensions", "open"]);

            if (fields[5].IsNullOrEmpty)
            {
                return Error.NotFound("auction.lot.gate_not_open", "This lot is not open for bidding.")
                    .With("lotId", lotId);
            }

            var amount = (long)fields[0];

            return new BidGateSnapshot(
                amount == 0 ? null : Money.FromMinorUnits(amount, Currency.Inr),
                ParseBidder(fields[1]),
                (long)fields[2],
                DateTimeOffset.FromUnixTimeMilliseconds((long)fields[3]),
                (int)fields[4],
                (int)fields[5] == 1);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Could not read the bid gate for lot {LotId}", lotId);
            return Unavailable();
        }
    }

    public async Task<Result> CloseAsync(Guid lotId, CancellationToken cancellationToken)
    {
        try
        {
            await _redis.GetDatabase().HashSetAsync(StateKey(lotId), "open", 0);
            return Result.Success();
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Could not close the bid gate for lot {LotId}", lotId);
            return Unavailable();
        }
    }

    /// <summary>
    /// Turns the script's positional array into a decision.
    /// </summary>
    /// <remarks>
    /// A malformed reply is treated as unavailable rather than guessed at. The only way to get one is
    /// a script version mismatch during a rolling deploy, and interpreting a reply from a script we
    /// did not write is exactly how a bid gets accepted for the wrong amount.
    /// </remarks>
    private Result<BidAdmission> Interpret(RedisResult raw, BidAttempt attempt)
    {
        var fields = (RedisValue[]?)raw;

        if (fields is null || fields.Length < 8)
        {
            _logger.LogError("Bid gate returned an unreadable reply for lot {LotId}", attempt.LotId);
            return Unavailable();
        }

        var outcome = (string?)fields[0] switch
        {
            "accepted" => BidOutcome.Accepted,
            "outbid" => BidOutcome.Outbid,
            "too_low" => BidOutcome.TooLow,
            "closed" => BidOutcome.Closed,
            _ => BidOutcome.Closed,
        };

        var wasExtended = (string?)fields[6] == "1";
        var isReplay = (string?)fields[7] == "1";
        var standing = (long)fields[1];
        var currency = attempt.Amount.Currency;

        return new BidAdmission
        {
            Outcome = isReplay ? BidOutcome.Replay : outcome,
            StandingAmount = Money.FromMinorUnits(standing, currency),
            StandingBidderId = ParseBidder(fields[2]) ?? Guid.Empty,
            Sequence = (long)fields[3],
            PlacedAt = attempt.PlacedAt,
            ExtendedTo = wasExtended
                ? DateTimeOffset.FromUnixTimeMilliseconds((long)fields[4])
                : null,
            ExtensionCount = (int)fields[5],
            IsReplay = isReplay,
        };
    }

    private static Guid? ParseBidder(RedisValue value)
        => Guid.TryParseExact((string?)value, "N", out var bidderId) ? bidderId : null;

    private static RedisKey StateKey(Guid lotId)
        => $"{KeyPrefix}{lotId:N}}}:state";

    private static RedisKey IdempotencyKey(Guid lotId, string idempotencyKey)
        => $"{KeyPrefix}{lotId:N}}}:idem:{idempotencyKey}";

    private static Error Unavailable()
        => Error.Upstream(
            "auction.bid_gate_unavailable",
            "Bidding is temporarily unavailable. Your bid was not placed — please try again.");
}
