using System.Net;
using System.Text.Json;
using PowerSave.Application.DTOs;

namespace Power_Save.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate _next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        this._next = _next;
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
            var incidentId = Guid.NewGuid().ToString("N");
            _logger.LogError(ex, "Unhandled exception occurred. IncidentId: {IncidentId}", incidentId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse.Fail("حدث خطأ غير متوقع في الخادم. يرجى المحاولة لاحقاً.", errorCode: "INTERNAL_SERVER_ERROR");
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}

