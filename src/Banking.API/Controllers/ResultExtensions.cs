using Banking.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

internal static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return controller.ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    public static ActionResult ToErrorResult(this ControllerBase controller, string? errorCode, string? errorMessage)
    {
        var problemDetails = new ProblemDetails
        {
            Detail = errorMessage,
            Title = "Request failed."
        };

        return errorCode switch
        {
            ErrorCodes.Validation => controller.BadRequest(problemDetails),
            ErrorCodes.Unauthorized => controller.Unauthorized(problemDetails),
            ErrorCodes.NotFound => controller.NotFound(problemDetails),
            ErrorCodes.Conflict => controller.Conflict(problemDetails),
            ErrorCodes.BusinessRule => controller.UnprocessableEntity(problemDetails),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, problemDetails)
        };
    }
}
