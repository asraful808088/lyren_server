using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sql.Services;

namespace Sql.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

       
        [Authorize(Roles = "Admin")]
        [HttpPost("load-cache")]
        public async Task<ActionResult<ApiResponse<object>>> LoadCache()
        {
            try
            {
                var count = await _productService.LoadAllProductsToRedisAsync();
                _logger.LogInformation($"✅ Loaded {count} products into Redis cache");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"{count} products loaded into Redis successfully",
                    Data = new { count },
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error loading products into Redis: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to load products into Redis",
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Invalid request",
                });

            try
            {
                var product = await _productService.CreateProductAsync(request);
                _logger.LogInformation($"✅ Product created: {product.Name}");

                return CreatedAtAction(nameof(GetProductById), new { id = product.Id },
                    new ApiResponse<ProductResponse>
                    {
                        Success = true,
                        Message = "Product created successfully",
                        Data = product,
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error creating product: {ex.Message}");
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Failed to create product",
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProductById(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found",
                    });

                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = product,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching product: {ex.Message}");
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Failed to fetch product",
                });
            }
        }

        
        [HttpGet("by-code/{encodedId}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProductByEncodedId(string encodedId)
        {
            try
            {
                var product = await _productService.GetProductByEncodedIdAsync(encodedId);
                if (product == null)
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found",
                    });

                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = product,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching product by code: {ex.Message}");
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Failed to fetch product",
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductResponse>>>> GetAllProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var products = await _productService.GetAllProductsAsync(page, pageSize);
                return Ok(new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = products,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching products: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = false,
                    Message = "Failed to fetch products",
                });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductResponse>>>> SearchProducts(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest(new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = false,
                    Message = "Search query is required",
                });

            try
            {
                var products = await _productService.SearchProductsAsync(query, page, pageSize);
                return Ok(new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = true,
                    Message = "Search results retrieved successfully",
                    Data = products,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error searching products: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = false,
                    Message = "Failed to search products",
                });
            }
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductResponse>>>> GetByCategory(
            string category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryAsync(category, page, pageSize);
                return Ok(new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = true,
                    Message = $"Products in category '{category}' retrieved successfully",
                    Data = products,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching products by category: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = false,
                    Message = "Failed to fetch products",
                });
            }
        }

        [HttpGet("collection/{collection}")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductResponse>>>> GetByCollection(
            string collection,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var products = await _productService.GetProductsByCollectionAsync(collection, page, pageSize);
                return Ok(new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = true,
                    Message = $"Products in collection '{collection}' retrieved successfully",
                    Data = products,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching products by collection: {ex.Message}");
                return StatusCode(500, new ApiResponse<PaginatedResponse<ProductResponse>>
                {
                    Success = false,
                    Message = "Failed to fetch products",
                });
            }
        }

        // 🔥 NEW: trending products (3 random items picked at cache warm-up time)
        [HttpGet("trending")]
        public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> GetTrendingProducts()
        {
            try
            {
                var products = await _productService.GetTrendingProductsAsync();
                return Ok(new ApiResponse<List<ProductResponse>>
                {
                    Success = true,
                    Message = "Trending products retrieved successfully",
                    Data = products,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching trending products: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<ProductResponse>>
                {
                    Success = false,
                    Message = "Failed to fetch trending products",
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateProduct(
            int id,
            [FromBody] UpdateProductRequest request)
        {
            try
            {
                var product = await _productService.UpdateProductAsync(id, request);
                if (product == null)
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found",
                    });

                _logger.LogInformation($"✅ Product updated: {product.Name}");
                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = product,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error updating product: {ex.Message}");
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Failed to update product",
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);
                if (!success)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Product not found",
                    });

                _logger.LogInformation($"✅ Product deleted: ID {id}");
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Product deleted successfully",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error deleting product: {ex.Message}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to delete product",
                });
            }
        }
    }
}