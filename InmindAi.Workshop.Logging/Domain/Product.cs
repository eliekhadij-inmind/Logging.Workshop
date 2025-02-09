namespace InmindAi.Workshop.Logging.Domain;

public sealed class Product
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product()
    {
        
    }

    public Product(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}
