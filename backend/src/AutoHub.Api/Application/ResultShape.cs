using System.Collections.Concurrent;
using System.Reflection;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Api.Application;

/// <summary>
/// Builds a failed <see cref="Result"/> or <see cref="Result{T}"/> for a response type known only
/// as a generic parameter.
/// </summary>
/// <remarks>
/// This exists so a pipeline behaviour can <b>return</b> a failure rather than throw one. Throwing
/// works and is what most FluentValidation samples do, and it is wrong here for two reasons: an
/// exception makes an expected outcome — "this phone number is malformed" — cost a stack unwind on
/// a hot path, and it routes a validation failure through the unhandled-exception handler, which is
/// the code path that is supposed to mean "we have a bug".
/// <para>
/// The reflection happens once per closed response type and is cached in a static generic field, so
/// the per-request cost is a single delegate invocation. <see cref="Result"/> is a readonly struct
/// with no common non-generic interface — deliberately, so it cannot be boxed by accident — which is
/// why there is no way to do this without one lookup somewhere.
/// </para>
/// </remarks>
internal static class ResultShape
{
    private static readonly ConcurrentDictionary<Type, Delegate?> Factories = new();

    /// <summary>
    /// Produces a failure of <typeparamref name="TResponse"/>, or reports that the type is not a
    /// <see cref="Result"/> at all.
    /// </summary>
    /// <remarks>
    /// A handler whose response is not a Result is a handler this behaviour has nothing useful to
    /// say about; the caller decides what to do, which in practice is "let it through and let the
    /// endpoint deal with it" rather than silently swallowing the request.
    /// </remarks>
    public static bool TryFailure<TResponse>(Error error, out TResponse response)
    {
        var factory = Factories.GetOrAdd(typeof(TResponse), Build);

        if (factory is Func<Error, TResponse> typed)
        {
            response = typed(error);
            return true;
        }

        response = default!;
        return false;
    }

    private static Delegate? Build(Type responseType)
    {
        if (responseType == typeof(Result))
        {
            return new Func<Error, Result>(Result.Failure);
        }

        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return null;
        }

        var method = responseType.GetMethod(
            nameof(Result<object>.Failure),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(Error)]);

        // Closing Func<Error, TResponse> over the discovered method turns the reflection into a
        // one-off: every later call is a delegate invocation, not a MethodInfo.Invoke.
        return method?.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(Error), responseType));
    }
}
