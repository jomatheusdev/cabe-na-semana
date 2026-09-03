namespace CabeNaSemana.Domain.Tasks;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Os dados informados são inválidos.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static DomainValidationException For(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
