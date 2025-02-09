namespace InmindAi.Workshop.Logging.Domain;

public sealed class Order
{
    private readonly HashSet<OrderLine> _orderLines = [];
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Reference { get; private set; } = string.Empty;
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines;

    private Order()
    {
    }

    public static Order CreatOrder(string reference, IEnumerable<OrderLine> orderLines)
    {
        var order = new Order
        {
            Reference = reference
        };

        foreach (var orderLine in orderLines)
        {
            order.AddOrderLine(orderLine);
        }

        return order;
    }

    public void AddOrderLine(OrderLine orderLine)
    {
        _orderLines.Add(orderLine);
    }
}
