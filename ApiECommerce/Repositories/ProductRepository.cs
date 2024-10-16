using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiECommerce.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _dbContext;

        public ProductRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IEnumerable<Product>> GetBestSellingProductsAsync()
        {
            return await _dbContext.Products
                .AsNoTracking()
                        .Where(p => p.Popular)
                        .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetPopularProductsAsync()
        {
            return await _dbContext.Products
                .AsNoTracking()
                       .Where(p => p.BestSeller)
                       .ToListAsync();
        }

        public async Task<Product> GetProductDetailsAsync(int id)
        {
            var productDetail = await _dbContext.Products.AsNoTracking()
                                            .FirstOrDefaultAsync(p => p.Id == id);

            if (productDetail is null)
                throw new InvalidOperationException();

            return productDetail;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbContext.Products
                       .Where(p => p.CategoryId == categoryId)
                       .ToListAsync();
        }
    }
}
