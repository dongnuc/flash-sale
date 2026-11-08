using FlashSaleStore.Contract.Abstractions.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlashSaleStore.Presentation.Abstractions;

public abstract class ApiEndpoint
{
    protected static IResult Handle<TValue>(
        Result<TValue> result,
        int successStatusCode = StatusCodes.Status200OK,
        Func<TValue, string>? locationFactory = null)
    {
        if (result.IsFailure) return HandlerFailure(result);

        if (successStatusCode == StatusCodes.Status201Created && locationFactory is not null)
        {
            return Results.Created(locationFactory(result.Value!), result);
        }

        return Results.Json(result, statusCode: successStatusCode);
    }

    protected static IResult HandlerFailure(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be mapped as an error.");
        }

        var statusCode = GetStatusCode(result.Error);
        var title = statusCode switch
        {
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ when result is IValidationResult => "Validation Error",
            _ => "Bad Request"
        };

        var errors = result is IValidationResult validationResult ? validationResult.Errors : null;
        return Results.Problem(CreateProblemDetails(title, statusCode, result.Error, errors));
    }

    private static ProblemDetails CreateProblemDetails(string title, int status, Error error, Error[]? errors = null) => new()
    {
        Title = title,
        Type = error.Code,
        Detail = error.Message,
        Status = status,
        Extensions =
        {
            ["code"] = error.Code,
            ["errors"] = errors ?? Array.Empty<Error>()
        }
    };

    private static int GetStatusCode(Error error)
    {
        if (error.Code.Contains("NotConfigured", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        if (error.Code.StartsWith("Payment.Provider.", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status502BadGateway;
        }

        if (error.Code.Contains("SignatureInvalid", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("AmountMismatch", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status400BadRequest;
        }

        if (error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status404NotFound;
        }

        if (error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Insufficient", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("BelowReserved", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("BatchUpdateFailed", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Overlap", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("HasReservations", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("NotPayable", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("DeadlineExceeded", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("TransitionInvalid", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("StatusInvalid", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Immutable", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("UserLimit.Exceeded", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains(".Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
