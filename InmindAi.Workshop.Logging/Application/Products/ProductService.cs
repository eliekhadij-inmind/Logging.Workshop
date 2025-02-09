using InmindAi.Workshop.Logging.Application.Contracts.Dtos;
using InmindAi.Workshop.Logging.Application.Contracts.Services;
using InmindAi.Workshop.Logging.Domain;
using InmindAi.Workshop.Logging.Errors;
using InmindAi.Workshop.Logging.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InmindAi.Workshop.Logging.Application.Products;

public class ProductService(WorkShopDbContext context, ILogger<ProductService> logger) : IProductService
{
    private readonly WorkShopDbContext _context = context;
    private readonly ILogger<ProductService> _logger = logger;
    public async Task<ProductDto> GetProductAsync(Guid productId)
    {
        var product = await _context.Products.FindAsync(productId);

        if (product is null)
        {
            _logger.LogWarning("Product with Id: {ProductId} was not found.", productId);
            throw new NotFoundException(ProductsErrorCodes.ProductNotFound);
        }
        return new ProductDto(product.Id, product.Name, product.Price); 
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        var products = await _context.Products.ToListAsync();
        return products.Select(x => new ProductDto(x.Id, x.Name, x.Price));
    }
}
