using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Application.Common;

public enum OperationFailureKind
{
    Validation,
    NotFound
}

public class OperationResult
{
    protected OperationResult(
        bool isSuccess,
        OperationFailureKind? failureKind,
        IReadOnlyDictionary<string, string[]> errors)
    {
        IsSuccess = isSuccess;
        FailureKind = failureKind;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public OperationFailureKind? FailureKind { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static OperationResult Success() => new(true, null, EmptyErrors);

    public static OperationResult<T> Success<T>(T value) =>
        new(true, value, null, EmptyErrors);

    public static OperationResult Failure(string field, string error) =>
        new(
            false,
            OperationFailureKind.Validation,
            new Dictionary<string, string[]> { [field] = [error] });

    public static OperationResult NotFound(string error) =>
        new(
            false,
            OperationFailureKind.NotFound,
            new Dictionary<string, string[]> { [string.Empty] = [error] });

    public static OperationResult From(DomainValidationException exception) =>
        new(false, OperationFailureKind.Validation, exception.Errors);

    public static OperationResult<T> From<T>(DomainValidationException exception) =>
        new(false, default, OperationFailureKind.Validation, exception.Errors);

    protected static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } =
        new Dictionary<string, string[]>();
}

public sealed class OperationResult<T> : OperationResult
{
    internal OperationResult(
        bool isSuccess,
        T? value,
        OperationFailureKind? failureKind,
        IReadOnlyDictionary<string, string[]> errors)
        : base(isSuccess, failureKind, errors)
    {
        Value = value;
    }

    public T? Value { get; }
}
