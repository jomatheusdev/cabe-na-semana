namespace CabeNaSemana.Application.Common;

public sealed class SystemAppClock : IAppClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
