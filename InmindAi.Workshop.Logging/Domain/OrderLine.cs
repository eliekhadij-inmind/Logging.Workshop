namespace InmindAi.Workshop.Logging.Domain;

public sealed class OrderLine
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    private OrderLine()
    {
    }
    public OrderLine(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }
}
