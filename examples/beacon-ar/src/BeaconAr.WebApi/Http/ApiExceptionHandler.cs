using System.Diagnostics;
using BeaconAr.Domain.Access.Application;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Sales.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Http;

public sealed partial class ApiExceptionHandler : IExceptionHandler
{
    #region Fields

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    #endregion

    #region Constructors

    public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;

        (int status, string code, string title, string detail, object? errors) = Map(exception);
        if (status >= 500)
            LogUnhandled(_logger, exception, code, Activity.Current?.Id ?? httpContext.TraceIdentifier);
        else
            LogExpected(_logger, code, Activity.Current?.Id ?? httpContext.TraceIdentifier);

        ProblemDetails problem = new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (errors is not null)
            problem.Extensions["errors"] = errors;
        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    #endregion

    #region Private Methods

    private static (int, string, string, string, object?) Map(Exception exception)
    {
        return exception switch
        {
            MasterDataValidationException value => (400, value.Code, value.SafeMessage, "The request could not be processed.", value.Errors),
            SalesValidationException value => (400, value.Code, value.SafeMessage, "The request could not be processed.", value.Errors),
            ApiBoundaryException value => (value.Status, value.Code, Title(value.Status), value.SafeMessage, null),
            AccessException value => (403, value.Code, "Forbidden", value.SafeMessage, null),
            MasterDataException value => (Status(value.Code), value.Code, Title(Status(value.Code)), value.SafeMessage, null),
            SalesException value => (Status(value.Code), value.Code, Title(Status(value.Code)), value.SafeMessage, null),
            BadHttpRequestException value when value.StatusCode == StatusCodes.Status413PayloadTooLarge => (413, "request_too_large", "Request too large", "The request body exceeds the configured limit.", null),
            BadHttpRequestException => (400, "malformed_request", "Bad request", "The request could not be read.", null),
            _ => (500, "internal_error", "Internal server error", "An unexpected error occurred.", null),
        };
    }

    private static int Status(string code) => code switch
    {
        "not_found" => 404,
        "concurrency_conflict" => 412,
        "duplicate_key" or "referenced_record" or "referenced_address" or "reference_conflict" or
            "invalid_quote_transition" or "invalid_sales_order_transition" => 409,
        _ => 409,
    };

    private static string Title(int status) => status switch
    {
        400 => "Bad request",
        403 => "Forbidden",
        404 => "Not found",
        409 => "Conflict",
        412 => "Precondition failed",
        413 => "Request too large",
        428 => "Precondition required",
        _ => "Request failed",
    };

    [LoggerMessage(LogLevel.Error, "Unhandled API error {code} with trace {traceId}.")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string code, string traceId);

    [LoggerMessage(LogLevel.Warning, "API request failed with {code} and trace {traceId}.")]
    private static partial void LogExpected(ILogger logger, string code, string traceId);

    #endregion
}
