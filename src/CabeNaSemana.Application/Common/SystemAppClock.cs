namespace CabeNaSemana.Application.Common;

public sealed class SystemAppClock(TimeProvider timeProvider, TimeZoneInfo timeZone) : IAppClock
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
}
