using CabeNaSemana.Domain.Planning;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Web.Models;

public static class UiLabels
{
    public static string Column(KanbanColumn status) => status switch
    {
        KanbanColumn.Planning => "A planejar",
        KanbanColumn.ThisWeek => "Esta semana",
        KanbanColumn.InProgress => "Em andamento",
        KanbanColumn.Completed => "Concluído",
        _ => "Desconhecido"
    };

    public static string ColumnHint(KanbanColumn status) => status switch
    {
        KanbanColumn.Planning => "Ainda fora da carga semanal",
        KanbanColumn.ThisWeek => "Compromissos assumidos",
        KanbanColumn.InProgress => "Foco agora",
        KanbanColumn.Completed => "Progresso conquistado",
        _ => string.Empty
    };

    public static string Importance(Importance importance) => importance switch
    {
        Domain.Tasks.Importance.Low => "Baixa",
        Domain.Tasks.Importance.Medium => "Média",
        Domain.Tasks.Importance.High => "Alta",
        Domain.Tasks.Importance.Critical => "Crítica",
        _ => "Desconhecida"
    };

    public static string Priority(PriorityLevel priority) => priority switch
    {
        PriorityLevel.Urgent => "Urgente",
        PriorityLevel.High => "Alta",
        PriorityLevel.Medium => "Média",
        PriorityLevel.Low => "Baixa",
        _ => "Concluída"
    };

    public static string Capacity(CapacityFit fit) => fit switch
    {
        CapacityFit.Fits => "Cabe na semana",
        CapacityFit.NearLimit => "Perto do limite",
        CapacityFit.Overflow => "Fora da capacidade",
        CapacityFit.Completed => "Concluída",
        _ => "Ainda não planejada"
    };

    public static string Deadline(DateOnly dueDate, int daysUntilDue) => daysUntilDue switch
    {
        < 0 => $"Atrasada · {dueDate:dd/MM}",
        0 => "Vence hoje",
        1 => "Vence amanhã",
        _ => $"{dueDate:dd/MM} · em {daysUntilDue} dias"
    };

    public static string CssToken<TEnum>(TEnum value) where TEnum : struct, Enum =>
        value.ToString().ToLowerInvariant();
}
