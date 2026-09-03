namespace CabeNaSemana.Application.Common;

public interface IAppClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
