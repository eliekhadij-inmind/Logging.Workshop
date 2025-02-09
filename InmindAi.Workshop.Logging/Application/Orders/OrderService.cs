using EntityFramework.Exceptions.Common;
using InmindAi.Workshop.Logging.Application.Contracts.Dtos;
using InmindAi.Workshop.Logging.Application.Contracts.Services;
using InmindAi.Workshop.Logging.Domain;
using InmindAi.Workshop.Logging.Errors;
using InmindAi.Workshop.Logging.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InmindAi.Workshop.Logging.Application.Orders;

public class OrderService(WorkShopDbContext context, ILogger<OrderService> logger) : IOrderService
{
    private readonly WorkShopDbContext _context = context;
    private readonly ILogger<OrderService> _logger = logger;
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto)
    {
        var order = Order.CreatOrder(orderDto.Reference, orderDto.OrderLines.Select(x => new OrderLine(x.ProductId, x.Quantity)));
        var entity = await _context.Orders.AddAsync(order);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (UniqueConstraintException ex)
        {
            _logger.LogWarning("An order with the same reference already exists: {Reference}, {Message}", order.Reference, ex.Message);
            throw new ServiceException(StatusCodes.Status403Forbidden, OrdersErrorCodes.OrderWithSameReferenceAlreadyExists);
        }
        _logger.LogInformation("An order has been created succesfully with Id: {Id}", order.Id);
        return new OrderDto(entity.Entity.Id, entity.Entity.Reference, entity.Entity.OrderLines.Select(x => new OrderLineDto(x.ProductId, x.Quantity)));
    }

    public async Task DeleteOrderAsync(Guid orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("Unable to find order with Id: {Id}", orderId);
            throw new NotFoundException(OrdersErrorCodes.OrderNotFound);
        }
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        _logger.LogInformation("An order has been deleted succesfully with Id: {Id}", order.Id);
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId)
    {
        var order = await _context.Orders.Include(x => x.OrderLines).FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null)
        {
            _logger.LogWarning("Unable to find order with Id: {Id}", orderId);
            throw new InvalidOperationException($"Order with Id: {orderId} Not Found");
        }
        return new OrderDto(order.Id, order.Reference, order.OrderLines.Select(x => new OrderLineDto(x.ProductId, x.Quantity)));
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync()
    {
        var orders = await _context.Orders.Include(x => x.OrderLines).ToListAsync();
        return orders.Select(x => new OrderDto(x.Id, x.Reference, x.OrderLines.Select(x => new OrderLineDto(x.ProductId, x.Quantity))));
    }

    public async Task<OrderDto> UpdateOrderAsync(Guid orderId, IEnumerable<OrderLineDto> orderLines)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order is null)
        {
            _logger.LogWarning("Unable to find order with Id: {Id}", orderId);
            throw new InvalidOperationException($"Order with Id: {orderId} Not Found");
        }
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return new OrderDto(order.Id, order.Reference, order.OrderLines.Select(x => new OrderLineDto(x.ProductId, x.Quantity)));
    }
}
