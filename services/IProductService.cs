using Sql.Models;

namespace Sql.Services
{
    public interface IProductService
    {
        Task<int> LoadAllProductsToRedisAsync();
        Task<ProductResponse> CreateProductAsync(CreateProductRequest request);
        Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request);
        Task<ProductResponse?> GetProductByIdAsync(int id);
        Task<ProductResponse?> GetProductByEncodedIdAsync(string encodedId);
        Task<PaginatedResponse<ProductResponse>> GetAllProductsAsync(int page = 1, int pageSize = 12);
        Task<PaginatedResponse<ProductResponse>> SearchProductsAsync(string query, int page = 1, int pageSize = 12);
        Task<PaginatedResponse<ProductResponse>> GetProductsByCategoryAsync(string category, int page = 1, int pageSize = 12);
        Task<PaginatedResponse<ProductResponse>> GetProductsByCollectionAsync(string collection, int page = 1, int pageSize = 12);
        Task<bool> DeleteProductAsync(int id);
        Task<List<ReviewDto>> GetProductReviewsAsync(int productId);
        Task<ReviewDto> AddReviewAsync(int productId, ReviewDto reviewDto);
        Task<List<ProductResponse>> GetTrendingProductsAsync();
    }
}