using CabeNaSemana.Api.Contracts;
using CabeNaSemana.Application.Board;
using CabeNaSemana.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CabeNaSemana.Api.Controllers;

[Route("api/tasks")]
public sealed class TasksController(BoardService boardService) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await boardService.FindAsync(id, cancellationToken);
        if (task is null)
        {
            return Failure(OperationResult.NotFound("Atividade não encontrada."));
        }

        return Ok(TaskResponse.From(task));
    }

    [HttpPost]
    [ProducesResponseType<CreatedTaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CreatedTaskResponse>> Create(
        SaveTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await boardService.CreateAsync(ToCommand(request), cancellationToken);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        var response = new CreatedTaskResponse(result.Value);
        return Created($"/api/tasks/{response.Id}", response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        SaveTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await boardService.UpdateAsync(id, ToCommand(request), cancellationToken);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Move(
        Guid id,
        MoveTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await boardService.MoveAsync(id, request.Status!.Value, cancellationToken);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await boardService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    private static SaveTaskCommand ToCommand(SaveTaskRequest request) => new(
        request.Title!,
        request.DueDate!.Value,
        request.Importance!.Value,
        request.EstimatedHours!.Value,
        request.Status!.Value);
}
