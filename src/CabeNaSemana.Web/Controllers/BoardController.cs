using CabeNaSemana.Application.Board;
using CabeNaSemana.Application.Common;
using CabeNaSemana.Domain.Tasks;
using CabeNaSemana.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CabeNaSemana.Web.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class BoardController(BoardService boardService, ILogger<BoardController> logger)
    : Controller
{
    private static readonly Action<ILogger, int?, Exception?> LogErrorPageShown =
        LoggerMessage.Define<int?>(
            LogLevel.Warning,
            new EventId(1001, nameof(Error)),
            "Página de erro exibida com status {StatusCode}.");

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var board = await boardService.GetAsync(cancellationToken);
        return View(new BoardPageViewModel(
            board,
            new TaskFormModel
            {
                DueDate = board.Today.AddDays(7),
                Importance = Importance.Medium,
                EstimatedHours = 2m,
                Status = KanbanColumn.Planning
            }));
    }

    [HttpPost("tarefas")]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "NewTask")] TaskFormModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RenderBoardWithErrorsAsync(form, cancellationToken);
        }

        var result = await boardService.CreateAsync(ToCommand(form), cancellationToken);
        if (!result.IsSuccess)
        {
            AddErrors(result, "NewTask.");
            return await RenderBoardWithErrorsAsync(form, cancellationToken);
        }

        TempData["SuccessMessage"] = "Atividade adicionada ao plano.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("tarefas/{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var task = await boardService.FindAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        return View(new EditTaskViewModel(
            task.Id,
            new TaskFormModel
            {
                Title = task.Title,
                DueDate = task.DueDate,
                Importance = task.Importance,
                EstimatedHours = task.EstimatedHours,
                Status = task.Status
            }));
    }

    [HttpPost("tarefas/{id:guid}/editar")]
    public async Task<IActionResult> Edit(
        Guid id,
        [Bind(Prefix = "Task")] TaskFormModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(new EditTaskViewModel(id, form));
        }

        var result = await boardService.UpdateAsync(id, ToCommand(form), cancellationToken);
        if (!result.IsSuccess)
        {
            AddErrors(result, "Task.");
            return View(new EditTaskViewModel(id, form));
        }

        TempData["SuccessMessage"] = "Atividade atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("tarefas/{id:guid}/mover")]
    public async Task<IActionResult> Move(
        Guid id,
        KanbanColumn status,
        CancellationToken cancellationToken)
    {
        var result = await boardService.MoveAsync(id, status, cancellationToken);
        SetFeedback(result, "Atividade movida no quadro.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("tarefas/{id:guid}/excluir")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await boardService.DeleteAsync(id, cancellationToken);
        SetFeedback(result, "Atividade excluída.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("capacidade")]
    public async Task<IActionResult> SetCapacity(
        CapacityFormModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "A capacidade deve ficar entre 1 e 80 horas.";
            return RedirectToAction(nameof(Index));
        }

        var result = await boardService.SetCapacityAsync(form.WeeklyCapacity, cancellationToken);
        SetFeedback(result, "Capacidade semanal atualizada.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("erro")]
    [IgnoreAntiforgeryToken]
    public IActionResult Error(int? statusCode)
    {
        LogErrorPageShown(logger, statusCode, null);
        Response.StatusCode = statusCode is >= 400 and <= 599 ? statusCode.Value : 500;
        return View("Error", statusCode);
    }

    [HttpGet("pagina-nao-encontrada")]
    [IgnoreAntiforgeryToken]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("Error", StatusCodes.Status404NotFound);
    }

    private async Task<IActionResult> RenderBoardWithErrorsAsync(
        TaskFormModel form,
        CancellationToken cancellationToken)
    {
        var board = await boardService.GetAsync(cancellationToken);
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View(nameof(Index), new BoardPageViewModel(board, form));
    }

    private void SetFeedback(OperationResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = successMessage;
            return;
        }

        TempData["ErrorMessage"] = result.Errors.Values.SelectMany(errors => errors).FirstOrDefault()
            ?? "Não foi possível concluir a ação.";
    }

    private void AddErrors(OperationResult result, string prefix)
    {
        foreach (var (field, errors) in result.Errors)
        {
            var key = string.IsNullOrEmpty(field) ? string.Empty : $"{prefix}{field}";
            foreach (var error in errors)
            {
                ModelState.AddModelError(key, error);
            }
        }
    }

    private static SaveTaskCommand ToCommand(TaskFormModel form) => new(
        form.Title,
        form.DueDate,
        form.Importance,
        form.EstimatedHours,
        form.Status);
}
