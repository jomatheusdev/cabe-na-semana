using System.ComponentModel.DataAnnotations;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Web.Models;

public sealed class TaskFormModel
{
    [Required(ErrorMessage = "Informe o título da atividade.")]
    [StringLength(
        StudyTask.MaximumTitleLength,
        MinimumLength = 1,
        ErrorMessage = "Use entre 1 e 120 caracteres.")]
    [Display(Name = "Atividade")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe o prazo.")]
    [DataType(DataType.Date)]
    [Display(Name = "Prazo")]
    public DateOnly DueDate { get; init; }

    [EnumDataType(typeof(Importance), ErrorMessage = "Selecione uma importância válida.")]
    [Display(Name = "Importância")]
    public Importance Importance { get; init; } = Importance.Medium;

    [Range(0.25, 100, ErrorMessage = "Informe entre 0,25 e 100 horas.")]
    [Display(Name = "Esforço estimado")]
    public decimal EstimatedHours { get; init; } = 2m;

    [EnumDataType(typeof(KanbanColumn), ErrorMessage = "Selecione uma coluna válida.")]
    [Display(Name = "Coluna")]
    public KanbanColumn Status { get; init; } = KanbanColumn.Planning;
}
