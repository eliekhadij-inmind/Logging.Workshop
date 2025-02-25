namespace InmindAi.Workshop.Logging.Correlation;

public interface ICorrelationIdGenerator
{
    public string Get();
    public void Set(string corrolationId);
}
