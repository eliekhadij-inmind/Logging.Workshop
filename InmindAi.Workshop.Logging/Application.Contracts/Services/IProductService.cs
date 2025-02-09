using InmindAi.Workshop.Logging.Application.Contracts.Dtos;

namespace InmindAi.Workshop.Logging.Application.Contracts.Services;

public interface IProductService
{
    Task<ProductDto> GetProductAsync(Guid productId);
    Task<IEnumerable<ProductDto>> GetProductsAsync();
}
