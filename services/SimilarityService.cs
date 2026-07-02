using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sql.Models;

namespace Sql.Services
{
    // ========================================
    // INTERFACE
    // ========================================

    public interface ISimilarityService
    {
        // Import operations
        Task<int> ImportSimilarityFromJsonAsync(string filePath);
        
        // Query operations
        Task<List<SimilarityResponse>> GetSimilarProductsAsync(int productId, int topN = 10);
        Task<SimilarityResponse?> GetSimilarityAsync(int productId, int similarProductId);
        Task<List<SimilarityResponse>> GetAllSimilaritiesAsync(int page = 1, int pageSize = 20);
        Task<int> GetTotalSimilaritiesCountAsync();
        
        // Utility
        Task<bool> ClearSimilaritiesAsync();
    }

    // ========================================
    // SERVICE IMPLEMENTATION
    // ========================================

    public class SimilarityService : ISimilarityService
    {
        private readonly ProductContext _context;
        private readonly ILogger<SimilarityService> _logger;

        // How many similarity rows to buffer before calling SaveChanges.
        // Keeps memory bounded and gives partial progress if interrupted.
        private const int BatchSize = 500;

        public SimilarityService(ProductContext context, ILogger<SimilarityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ────────────────────────────────────────────────────────────────────────────
        // IMPORT FROM JSON
        // ────────────────────────────────────────────────────────────────────────────

        public async Task<int> ImportSimilarityFromJsonAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"📂 Importing similarities from {filePath}...");

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                string jsonContent = await File.ReadAllTextAsync(filePath);

                using JsonDocument doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException("JSON root must be an array");

                // ── Step 1: Load ALL valid product IDs once, into memory. ──
                // Avoids one SELECT per product/similar lookup.
                var validProductIds = await _context.Products
                    .Select(p => p.Id)
                    .ToHashSetAsync();

                _logger.LogInformation($"📦 Loaded {validProductIds.Count} valid product IDs");

                // ── Step 2: Load ALL existing similarity pairs once, into memory. ──
                // Avoids one SELECT per pair-existence check.
                var existingPairs = await _context.Similarities
                    .Select(s => new { s.ProductId, s.SimilarProductId, s.Id })
                    .ToDictionaryAsync(s => (s.ProductId, s.SimilarProductId), s => s.Id);

                _logger.LogInformation($"📦 Loaded {existingPairs.Count} existing similarity rows");

                int importedCount = 0;
                int skippedCount = 0;
                int pendingInBatch = 0;
                var now = DateTime.UtcNow;

                foreach (JsonElement productElement in root.EnumerateArray())
                {
                    if (!productElement.TryGetProperty("id", out var idElement))
                        continue;

                    int productId = idElement.GetInt32();

                    if (!validProductIds.Contains(productId))
                    {
                        skippedCount++;
                        continue;
                    }

                    if (!productElement.TryGetProperty("similar", out var similarElement) ||
                        similarElement.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (JsonElement similarItem in similarElement.EnumerateArray())
                    {
                        if (!similarItem.TryGetProperty("id", out var similarIdElement) ||
                            !similarItem.TryGetProperty("score", out var scoreElement))
                            continue;

                        int similarProductId = similarIdElement.GetInt32();

                        if (!validProductIds.Contains(similarProductId))
                            continue;

                        decimal score = (decimal)scoreElement.GetDouble();
                        var key = (productId, similarProductId);

                        if (existingPairs.TryGetValue(key, out var existingId))
                        {
                            // Update in place via a tracked stub (avoids re-querying the row)
                            var stub = new Similarity { Id = existingId, ProductId = productId, SimilarProductId = similarProductId };
                            _context.Attach(stub);
                            stub.Rate = score;
                            stub.UpdateTime = now;
                            _context.Entry(stub).Property(x => x.Rate).IsModified = true;
                            _context.Entry(stub).Property(x => x.UpdateTime).IsModified = true;
                        }
                        else
                        {
                            _context.Similarities.Add(new Similarity
                            {
                                ProductId = productId,
                                SimilarProductId = similarProductId,
                                Rate = score,
                                CreateTime = now,
                                UpdateTime = now
                            });
                            existingPairs[key] = 0; // prevent duplicate inserts within this same run
                        }

                        importedCount++;
                        pendingInBatch++;

                        // ── Step 3: Save in batches so progress isn't lost. ──
                        if (pendingInBatch >= BatchSize)
                        {
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear(); // detach to keep memory/tracking fast
                            pendingInBatch = 0;
                            _logger.LogInformation($"💾 Saved batch — {importedCount} rows processed so far");
                        }
                    }
                }

                // Final flush for any remainder under the batch size
                if (pendingInBatch > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"✅ Import complete: {importedCount} similarities imported, {skippedCount} skipped");

                return importedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Import error: {ex.Message}");
                throw;
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // QUERY OPERATIONS
        // ────────────────────────────────────────────────────────────────────────────

        public async Task<List<SimilarityResponse>> GetSimilarProductsAsync(int productId, int topN = 10)
        {
            try
            {
                _logger.LogInformation($"🔍 Getting {topN} similar products for product {productId}...");

                var similarities = await _context.Similarities
                    .Where(s => s.ProductId == productId)
                    .OrderByDescending(s => s.Rate)
                    .Take(topN)
                    .Include(s => s.SimilarProduct)
                    .Select(s => new SimilarityResponse
                    {
                        Id = s.Id,
                        ProductId = s.ProductId,
                        SimilarProductId = s.SimilarProductId,
                        SimilarProductName = s.SimilarProduct.Name,
                        Rate = s.Rate,
                        CreateTime = s.CreateTime,
                        UpdateTime = s.UpdateTime
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ Found {similarities.Count} similar products");
                return similarities;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error getting similar products: {ex.Message}");
                throw;
            }
        }

        public async Task<SimilarityResponse?> GetSimilarityAsync(int productId, int similarProductId)
        {
            try
            {
                var similarity = await _context.Similarities
                    .Where(s => s.ProductId == productId && s.SimilarProductId == similarProductId)
                    .Include(s => s.SimilarProduct)
                    .Select(s => new SimilarityResponse
                    {
                        Id = s.Id,
                        ProductId = s.ProductId,
                        SimilarProductId = s.SimilarProductId,
                        SimilarProductName = s.SimilarProduct.Name,
                        Rate = s.Rate,
                        CreateTime = s.CreateTime,
                        UpdateTime = s.UpdateTime
                    })
                    .FirstOrDefaultAsync();

                return similarity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error getting similarity: {ex.Message}");
                throw;
            }
        }

        public async Task<List<SimilarityResponse>> GetAllSimilaritiesAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var similarities = await _context.Similarities
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(s => s.SimilarProduct)
                    .Select(s => new SimilarityResponse
                    {
                        Id = s.Id,
                        ProductId = s.ProductId,
                        SimilarProductId = s.SimilarProductId,
                        SimilarProductName = s.SimilarProduct.Name,
                        Rate = s.Rate,
                        CreateTime = s.CreateTime,
                        UpdateTime = s.UpdateTime
                    })
                    .ToListAsync();

                return similarities;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error getting similarities: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetTotalSimilaritiesCountAsync()
        {
            return await _context.Similarities.CountAsync();
        }

        // ────────────────────────────────────────────────────────────────────────────
        // UTILITY
        // ────────────────────────────────────────────────────────────────────────────

        public async Task<bool> ClearSimilaritiesAsync()
        {
            try
            {
                _logger.LogWarning("🗑️  Clearing all similarities...");

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM similarity");

                _logger.LogInformation("✅ All similarities cleared");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error clearing similarities: {ex.Message}");
                return false;
            }
        }
    }
}