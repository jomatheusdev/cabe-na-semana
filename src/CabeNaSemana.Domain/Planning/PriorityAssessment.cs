namespace CabeNaSemana.Domain.Planning;

public enum PriorityLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public sealed record PriorityAssessment(
    int Score,
    PriorityLevel Level,
    string Explanation,
    int DaysUntilDue);
