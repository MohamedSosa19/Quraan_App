using System.Diagnostics.CodeAnalysis;

namespace Quraan.Domain.Common;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Domain Result-pattern type; consumed only from C# / TypeScript clients.")]
public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error NotFound(string what) => new("not_found", $"{what} not found.");
    public static Error Conflict(string detail) => new("conflict", detail);
    public static Error Validation(string detail) => new("validation", detail);
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Idiomatic Result<T> pattern: Result<T>.Success / Result<T>.Failure factory methods.")]
public readonly struct Result<T>
{
    public T? Value { get; }
    public Error Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        Error = Error.None;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
