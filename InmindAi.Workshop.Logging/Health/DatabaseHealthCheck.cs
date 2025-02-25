using InmindAi.Workshop.Logging.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InmindAi.Workshop.Logging.Health;

public class DatabaseHealthCheck(WorkShopDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {

        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Unable to connect the database.");
    }
}
