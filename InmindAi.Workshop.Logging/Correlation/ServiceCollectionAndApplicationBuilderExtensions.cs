namespace InmindAi.Workshop.Logging.Correlation;

public static class ServiceCollectionAndApplicationBuilderExtensions
{
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
