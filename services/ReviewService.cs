using Microsoft.EntityFrameworkCore;
using Sql.Models;
using Sql.Services;
namespace Sqls.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ProductContext _context;

        public ReviewService(ProductContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();

            return reviews.Select(MapToDto).ToList();
        }

        public async Task<ReviewDto> AddReviewAsync(int productId, CreateReviewRequest request, string username)
        {
            
            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.Username == username);

            if (existing != null)
                throw new InvalidOperationException("You have already reviewed this product.");

            if (request.Rating < 1 || request.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            var review = new Review
            {
                ProductId  = productId,
                Username   = username,
                Rating     = request.Rating,
                Comment    = request.Comment,
                React      = 0,
                CreateTime = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow,
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return MapToDto(review);
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string username, bool isAdmin)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

           
            if (!isAdmin && review.Username != username)
                throw new UnauthorizedAccessException("You can only delete your own reviews.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ReviewDto?> ReactToReviewAsync(int reviewId, int react)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return null;

            review.React      += react; // +1 like, -1 dislike
            review.UpdateTime  = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToDto(review);
        }

        private static ReviewDto MapToDto(Review r) => new()
        {
            Id         = r.Id,
            Username   = r.Username,
            Rating     = r.Rating,
            Comment    = r.Comment,
            React      = r.React,
            CreateTime = r.CreateTime,
        };
    }
}