using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Application.Common;

namespace OppSignal.Api.Infrastructure;

/// <summary>
/// Global error handling → RFC7807 ProblemDetails. Application exceptions map to
/// their status codes; validation errors to 400 with field errors; everything
/// else to 500 (no internal detail leaked in Production).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log, IHostEnvironment env)
    {
        _next = next;
        _log = log;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            };
            await WriteAsync(context, problem, StatusCodes.Status400BadRequest);
        }
        catch (AppException ex)
        {
            var problem = new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = ex.Title,
                Detail = ex.Message,
            };
            await WriteAsync(context, problem, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = _env.IsDevelopment() ? ex.ToString() : null,
            };
            await WriteAsync(context, problem, StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task WriteAsync(HttpContext context, ProblemDetails problem, int status)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
