using System.Text.Json;

namespace ECBCurrencyRates.Middleware
{
  public class ErrorHandlingMiddleware
  {
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
      try
      {
        await _next(context);
      }
      catch (Exception ex)
      {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var response = new { error = ex.Message };
        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
      }
    }
  }
}
