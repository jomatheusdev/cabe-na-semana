using CabeNaSemana.Application.Common;

namespace CabeNaSemana.Tests.Application;

public sealed class SystemAppClockTests
{
    [Fact]
    public void Today_UsesConfiguredBusinessTimeZoneInsteadOfMachineLocalTime()
    {
        var utcNow = new DateTimeOffset(2026, 9, 4, 1, 30, 0, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(utcNow);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Fortaleza");
        var clock = new SystemAppClock(timeProvider, timeZone);

        Assert.Equal(new DateOnly(2026, 9, 3), clock.Today);
        Assert.Equal(utcNow.UtcDateTime, clock.UtcNow);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
