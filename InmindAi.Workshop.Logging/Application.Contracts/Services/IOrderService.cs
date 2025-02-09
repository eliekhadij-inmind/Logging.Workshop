using InmindAi.Workshop.Logging.Application.Contracts.Dtos;

namespace InmindAi.Workshop.Logging.Application.Contracts.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto);
    Task<OrderDto> GetOrderAsync(Guid orderId);
    Task<IEnumerable<OrderDto>> GetOrdersAsync();
    Task<OrderDto> UpdateOrderAsync(Guid orderId, IEnumerable<OrderLineDto> orderLines);
    Task DeleteOrderAsync(Guid orderId);
}
