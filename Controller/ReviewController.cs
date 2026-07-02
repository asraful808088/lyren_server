using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Sql.Services;
using System.Security.Claims;
using Sqls.Services;


namespace Sql.Controllers
{
    [ApiController]
    [Route("api/product/{productId}/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetReviews(int productId)
        {
            try
            {
                var reviews = await _reviewService.GetProductReviewsAsync(productId);
                return Ok(new ApiResponse<List<ReviewDto>>
                {
                    Success = true,
                    Message = "Reviews retrieved successfully",
                    Data    = reviews,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching reviews: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<ReviewDto>>
                {
                    Success = false,
                    Message = "Failed to fetch reviews",
                });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> AddReview(
            int productId,
            [FromBody] CreateReviewRequest request)
        {
            
            var email    = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue("email")
                        ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "User identity not found",
                });

            
            var username = email.Contains('@') ? email.Split('@')[0] : email;

            try
            {
                var review = await _reviewService.AddReviewAsync(productId, request, username);
                _logger.LogInformation($"✅ Review added by {username} for product {productId}");

                return CreatedAtAction(nameof(GetReviews), new { productId },
                    new ApiResponse<ReviewDto>
                    {
                        Success = true,
                        Message = "Review added successfully",
                        Data    = review,
                    });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error adding review: {ex.Message}");
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "Failed to add review",
                });
            }
        }

        [Authorize]
        [HttpDelete("{reviewId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteReview(int productId, int reviewId)
        {
            var email    = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue("email")
                        ?? User.Identity?.Name ?? "";
            var username = email.Contains('@') ? email.Split('@')[0] : email;
            var isAdmin  = User.IsInRole("Admin");

            try
            {
                var success = await _reviewService.DeleteReviewAsync(reviewId, username, isAdmin);
                if (!success)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Review not found" });

                return Ok(new ApiResponse<object> { Success = true, Message = "Review deleted" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error deleting review: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Failed to delete review" });
            }
        }

        [HttpPost("{reviewId}/react")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> ReactToReview(
            int productId,
            int reviewId,
            [FromQuery] int value = 1)
        {
            try
            {
                var review = await _reviewService.ReactToReviewAsync(reviewId, value);
                if (review == null)
                    return NotFound(new ApiResponse<ReviewDto> { Success = false, Message = "Review not found" });

                return Ok(new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Message = "Reaction saved",
                    Data    = review,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error reacting to review: {ex.Message}");
                return StatusCode(500, new ApiResponse<ReviewDto> { Success = false, Message = "Failed to save reaction" });
            }
        }
    }
}