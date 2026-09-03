using CabeNaSemana.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CabeNaSemana.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult Failure(OperationResult result)
    {
        if (result.FailureKind == OperationFailureKind.NotFound)
        {
            var notFound = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Atividade não encontrada.",
                Detail = result.Errors.Values.SelectMany(errors => errors).FirstOrDefault()
            };
            return ProblemResult(notFound);
        }

        var errors = result.Errors.ToDictionary(
            pair => ToCamelCase(pair.Key),
            pair => pair.Value,
            StringComparer.Ordinal);
        var validation = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Os dados não atendem às regras do planejamento."
        };
        return ProblemResult(validation);
    }

    private static ObjectResult ProblemResult(ProblemDetails problem)
    {
        var result = new ObjectResult(problem) { StatusCode = problem.Status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private static string ToCamelCase(string field) =>
        string.IsNullOrEmpty(field)
            ? "request"
            : char.ToLowerInvariant(field[0]) + field[1..];
}
