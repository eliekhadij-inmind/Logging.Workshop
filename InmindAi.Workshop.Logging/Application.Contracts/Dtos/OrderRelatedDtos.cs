namespace InmindAi.Workshop.Logging.Application.Contracts.Dtos;

[Serializable]
public record OrderLineDto(Guid ProductId, int Quantity);

[Serializable]
public record OrderDto(Guid Id, string Reference, IEnumerable<OrderLineDto> OrderLines);

[Serializable]
public record CreateOrderDto(string Reference, IEnumerable<OrderLineDto> OrderLines);


