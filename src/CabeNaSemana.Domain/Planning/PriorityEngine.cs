using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Domain.Planning;

public static class PriorityEngine
{
    public static PriorityAssessment Assess(StudyTask task, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(task);

        var daysUntilDue = task.DueDate.DayNumber - today.DayNumber;
        if (task.Status == KanbanColumn.Completed)
        {
            return new PriorityAssessment(
                0,
                PriorityLevel.None,
                "Atividade concluída; não disputa capacidade da semana.",
                daysUntilDue);
        }

        var score = GetDeadlineScore(daysUntilDue)
            + GetImportanceScore(task.Importance)
            + GetEffortScore(task.EstimatedHours);

        return new PriorityAssessment(
            score,
            GetLevel(score),
            BuildExplanation(task.Importance, daysUntilDue, task.EstimatedHours),
            daysUntilDue);
    }

    private static int GetDeadlineScore(int daysUntilDue) => daysUntilDue switch
    {
        < 0 => 50,
        0 => 46,
        <= 2 => 40,
        <= 7 => 26,
        <= 14 => 14,
        _ => 5
    };

    private static int GetImportanceScore(Importance importance) => importance switch
    {
        Importance.Critical => 38,
        Importance.High => 28,
        Importance.Medium => 18,
        Importance.Low => 8,
        _ => 0
    };

    private static int GetEffortScore(decimal hours) => hours switch
    {
        <= 2m => 5,
        <= 5m => 3,
        <= 8m => 0,
        _ => -3
    };

    private static PriorityLevel GetLevel(int score) => score switch
    {
        >= 65 => PriorityLevel.Urgent,
        >= 45 => PriorityLevel.High,
        >= 25 => PriorityLevel.Medium,
        _ => PriorityLevel.Low
    };

    private static string BuildExplanation(Importance importance, int daysUntilDue, decimal hours)
    {
        var importanceText = importance switch
        {
            Importance.Critical => "Importância crítica",
            Importance.High => "Importância alta",
            Importance.Medium => "Importância média",
            _ => "Importância baixa"
        };
        var deadlineText = daysUntilDue switch
        {
            < 0 => $"atividade atrasada há {Math.Abs(daysUntilDue)} dia(s)",
            0 => "prazo hoje",
            1 => "prazo amanhã",
            <= 7 => $"prazo em {daysUntilDue} dias",
            _ => $"prazo em {daysUntilDue} dias"
        };
        var effortText = hours <= 2m ? "execução curta" : $"{hours:0.#} h de esforço";

        return $"{importanceText}, {deadlineText} e {effortText}.";
    }
}
