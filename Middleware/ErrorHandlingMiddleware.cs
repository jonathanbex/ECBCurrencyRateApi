using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ECBCurrencyRates.Middleware
{
  public class ErrorHandlingMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
      _next = next;
      _logger = logger;
      _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
      try
      {
        await _next(context);
      }
      catch (OperationCanceledException oce)
      {
        // Client disconnected / request cancelled
        _logger?.LogWarning(oce, "Request was canceled by the client.");

        // If the response has already started, we can't modify it
        if (context.Response.HasStarted)
        {
          _logger?.LogWarning("The response has already started, unable to write a cancellation response.");
          return;
        }

        // Try to set a numeric code that indicates client closed the request (non-standard 499)
        try
        {
          context.Response.Clear();
          context.Response.StatusCode = 499;
          context.Response.ContentType = "application/json";
          var payload = new { error = "Request was canceled by the client.", statusCode = context.Response.StatusCode };
          var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
          var json = JsonSerializer.Serialize(payload, opts);
          await context.Response.WriteAsync(json);
        }
        catch (System.Exception ex)
        {
          // If writing the cancellation response fails, just log and swallow since client already disconnected.
          _logger?.LogDebug(ex, "Failed to write cancellation response; client likely disconnected.");
        }
      }
      catch (Exception ex)
      {
        _logger?.LogError(ex, "Unhandled exception while processing request.");

        if (context.Response.HasStarted)
        {
          // If the response has already started, we cannot modify the headers/body. Re-throw so upstream can decide.
          _logger?.LogWarning("The response has already started, the error handling middleware will not modify the response.");
          throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        object response = _env != null && _env.IsDevelopment()
          ? new { error = ex.Message, detail = ex.StackTrace, statusCode = context.Response.StatusCode }
          : new { error = "An unexpected error occurred.", statusCode = context.Response.StatusCode };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
      }
    }
  }
}
