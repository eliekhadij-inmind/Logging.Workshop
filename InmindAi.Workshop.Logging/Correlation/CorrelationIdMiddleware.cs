
namespace InmindAi.Workshop.Logging.Correlation;

public class CorrelationIdMiddleware
{
    private const string _correlationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationIdGenerator = context.RequestServices.GetRequiredService<ICorrelationIdGenerator>();
        var correlationId = GetCorrelationIdTrace(context, correlationIdGenerator);
        AddCorrelationId(context, correlationId);
        await _next(context);
    }

    #region Private Methods

    private static string GetCorrelationIdTrace(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        var correlation = context.Request.Headers[_correlationIdHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(correlation))
        {
            correlationIdGenerator.Set(correlation);
            return correlation;
        }
        else
        {
            return correlationIdGenerator.Get();
        }
    }
    private void AddCorrelationId(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_correlationIdHeader))
            {
                context.Response.Headers.Add(_correlationIdHeader, new[] { correlationId });
            }          
            return Task.CompletedTask;
        });
    }

    #endregion
}
