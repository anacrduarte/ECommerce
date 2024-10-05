using ApiECommerce.Entities;
using ApiECommerce.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : Controller
    {
        private readonly IProductRepository _productRepository;

        public ProductsController(IProductRepository productRepository)
        {

            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(string productType, int? categoryId = null)
        {
            IEnumerable<Product> products;

            if (productType == "categoria" && categoryId != null)
            {
                products = await _productRepository.GetProductsByCategoryAsync(categoryId.Value);
            }
            else if (productType == "popular")
            {
                products = await _productRepository.GetPopularProductsAsync();
            }
            else if (productType == "maisvendido")
            {
                products = await _productRepository.GetBestSellingProductsAsync();
            }
            else
            {
                return BadRequest("Tipo de produto inválido");
            }

            var productData = products.Select(v => new
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price,
                UrlImage = v.UrlImage
            });

            return Ok(productData);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductDetail(int id)
        {
            var product = await _productRepository.GetProductDetailsAsync(id);

            if (product is null)
            {
                return NotFound($"Produto com id={id} não encontrado");
            }

            var productData = new
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Details = product.Details,
                UrlImage = product.UrlImage
            };

            return Ok(productData);
        }
    }
}
