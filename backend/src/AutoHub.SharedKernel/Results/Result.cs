using System.Diagnostics.CodeAnalysis;

namespace AutoHub.SharedKernel.Results;

/// <summary>
/// The outcome of an operation that is expected to be able to fail.
/// </summary>
/// <remarks>
/// Exceptions are reserved for bugs and for things genuinely outside the program's model
/// (a socket dying, a disk filling). "This dealer's KYC is not verified" is not exceptional —
/// it is Tuesday — so it travels as a <see cref="Result"/> and the compiler makes the caller
/// acknowledge it.
/// </remarks>
public readonly struct Result
{
    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    private readonly Error? _error;

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => _error
        ?? throw new InvalidOperationException("A successful Result has no Error.");

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Returns the first failure, or success if every result succeeded.</summary>
    public static Result FirstFailureOr(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }

    public static implicit operator Result(Error error) => Failure(error);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error);
}

/// <summary>The outcome of an operation that produces a value.</summary>
public readonly struct Result<T>
{
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    private readonly T? _value;
    private readonly Error? _error;

    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed Result has no Value. Error: {_error}");

    public Error Error => _error
        ?? throw new InvalidOperationException("A successful Result has no Error.");

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(Error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map)
        => IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(Error);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
        => IsSuccess ? bind(_value!) : Result<TOut>.Failure(Error);

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> bind)
        => IsSuccess ? await bind(_value!).ConfigureAwait(false) : Result<TOut>.Failure(Error);

    public Result<T> Ensure(Func<T, bool> predicate, Error error)
        => IsFailure || predicate(_value!) ? this : Failure(error);

    public Result ToResult() => IsSuccess ? Result.Success() : Result.Failure(Error);
}
