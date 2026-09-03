namespace CabeNaSemana.Infrastructure.Persistence;

public sealed class PlannerSetting
{
    public const int SingletonId = 1;

    public int Id { get; set; }
    public decimal WeeklyCapacityHours { get; set; }
}
