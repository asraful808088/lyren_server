using Microsoft.AspNetCore.Mvc;
using Sql.Models;
using Sql.Services;

namespace Sql.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimilarityController : ControllerBase
    {
        private readonly ISimilarityService _similarityService;
        private readonly ILogger<SimilarityController> _logger;

        public SimilarityController(ISimilarityService similarityService, ILogger<SimilarityController> logger)
        {
            _similarityService = similarityService;
            _logger = logger;
        }

        // ────────────────────────────────────────────────────────────────────────────
        // POST: api/similarity/import
        // ────────────────────────────────────────────────────────────────────────────
        [HttpPost("import")]
        public async Task<IActionResult> ImportSimilarities([FromBody] ImportSimilarityRequest request)
        {
            try
            {
                int importedCount = await _similarityService.ImportSimilarityFromJsonAsync(request.FilePath);

                return Ok(new
                {
                    success = true,
                    message = $"Imported {importedCount} similarities",
                    importedCount
                });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Import failed: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while importing similarities" });
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // GET: api/similarity/product/{productId}?topN=10
        // ────────────────────────────────────────────────────────────────────────────
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetSimilarProducts(int productId, [FromQuery] int topN = 10)
        {
            if (topN <= 0 || topN > 100)
                return BadRequest(new { success = false, message = "topN must be between 1 and 100" });

            var similarities = await _similarityService.GetSimilarProductsAsync(productId, topN);

            return Ok(new
            {
                success = true,
                productId,
                count = similarities.Count,
                data = similarities
            });
        }

        // ────────────────────────────────────────────────────────────────────────────
        // GET: api/similarity/{productId}/{similarProductId}
        // ────────────────────────────────────────────────────────────────────────────
        [HttpGet("{productId:int}/{similarProductId:int}")]
        public async Task<IActionResult> GetSimilarity(int productId, int similarProductId)
        {
            var similarity = await _similarityService.GetSimilarityAsync(productId, similarProductId);

            if (similarity == null)
                return NotFound(new { success = false, message = "Similarity not found" });

            return Ok(new { success = true, data = similarity });
        }

        // ────────────────────────────────────────────────────────────────────────────
        // GET: api/similarity?page=1&pageSize=20
        // ────────────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllSimilarities([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var similarities = await _similarityService.GetAllSimilaritiesAsync(page, pageSize);
            var totalCount = await _similarityService.GetTotalSimilaritiesCountAsync();

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = similarities
            });
        }

        // ────────────────────────────────────────────────────────────────────────────
        // DELETE: api/similarity/clear
        // ────────────────────────────────────────────────────────────────────────────
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearSimilarities()
        {
            bool cleared = await _similarityService.ClearSimilaritiesAsync();

            if (!cleared)
                return StatusCode(500, new { success = false, message = "Failed to clear similarities" });

            return Ok(new { success = true, message = "All similarities cleared" });
        }
    }
}