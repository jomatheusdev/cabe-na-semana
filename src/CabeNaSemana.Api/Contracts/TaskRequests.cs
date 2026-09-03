using System.ComponentModel.DataAnnotations;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Api.Contracts;

public sealed class SaveTaskRequest
{
    [Required(ErrorMessage = "Informe o título da atividade.")]
    [StringLength(
        StudyTask.MaximumTitleLength,
        MinimumLength = 1,
        ErrorMessage = "Use entre 1 e 120 caracteres.")]
    public string? Title { get; init; }

    [Required(ErrorMessage = "Informe o prazo.")]
    public DateOnly? DueDate { get; init; }

    [Required(ErrorMessage = "Informe a importância.")]
    [EnumDataType(typeof(Importance), ErrorMessage = "Selecione uma importância válida.")]
    public Importance? Importance { get; init; }

    [Required(ErrorMessage = "Informe o esforço estimado.")]
    [Range(0.25, 100, ErrorMessage = "Informe entre 0,25 e 100 horas.")]
    public decimal? EstimatedHours { get; init; }

    [Required(ErrorMessage = "Informe a coluna.")]
    [EnumDataType(typeof(KanbanColumn), ErrorMessage = "Selecione uma coluna válida.")]
    public KanbanColumn? Status { get; init; }
}

public sealed class MoveTaskRequest
{
    [Required(ErrorMessage = "Informe a coluna.")]
    [EnumDataType(typeof(KanbanColumn), ErrorMessage = "Selecione uma coluna válida.")]
    public KanbanColumn? Status { get; init; }
}
