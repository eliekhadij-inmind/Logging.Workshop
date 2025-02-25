namespace InmindAi.Workshop.Logging.Correlation;

public class CorrelationIdGenerator : ICorrelationIdGenerator
{
    private string _correlationId = Guid.CreateVersion7().ToString();
    public string Get() => _correlationId;

    public void Set(string corrolationId)
    {
        _correlationId = corrolationId;
    }
}
