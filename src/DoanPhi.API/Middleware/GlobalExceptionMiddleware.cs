using System.Net;
using System.Text.Json;
using DoanPhi.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DoanPhi.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã xảy ra lỗi không mong muốn: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            ValidationException valEx => new ValidationProblemDetails(valEx.Errors.ToDictionary(k => k.Key, v => v.Value))
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Dữ liệu khai báo không hợp lệ",
                Detail = valEx.Message
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Không tìm thấy dữ liệu",
                Detail = notFoundEx.Message
            },
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Lỗi máy chủ",
                Detail = "Đã xảy ra sự cố trong quá trình xử lý yêu cầu."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        var json = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(json);
    }
}
