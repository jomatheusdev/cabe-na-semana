using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Application.Common;

public sealed record OperationResult(
    bool IsSuccess,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static OperationResult Success() => new(true, EmptyErrors);

    public static OperationResult Failure(string field, string error) =>
        new(false, new Dictionary<string, string[]> { [field] = [error] });

    public static OperationResult From(DomainValidationException exception) =>
        new(false, exception.Errors);

    private static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } =
        new Dictionary<string, string[]>();
}
