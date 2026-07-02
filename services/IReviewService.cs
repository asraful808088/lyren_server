using Sql.Services;

namespace Sqls.Services
{
    public interface IReviewService
    {
        Task<List<ReviewDto>> GetProductReviewsAsync(int productId);
        Task<ReviewDto> AddReviewAsync(int productId, CreateReviewRequest request, string username);
        Task<bool> DeleteReviewAsync(int reviewId, string username, bool isAdmin);
        Task<ReviewDto?> ReactToReviewAsync(int reviewId, int react);
    }

    public class CreateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

   
}