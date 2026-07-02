using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sql.Models;
using Sql.Services;
using System.Text.Json;

namespace Sql.Controllers
{
   
    [ApiController]
    [Route("api/[controller]")]
    public class SeederController : ControllerBase
    {
        private readonly ProductContext         _context;
        private readonly server.ICloudinaryService _cloudinary;
        private readonly ILogger<SeederController> _logger;
        private readonly IWebHostEnvironment    _env;

        public SeederController(
            ProductContext context,
            server.ICloudinaryService cloudinary,
            ILogger<SeederController> logger,
            IWebHostEnvironment env)
        {
            _context   = context;
            _cloudinary = cloudinary;
            _logger    = logger;
            _env       = env;
        }

    
        [HttpGet("preview")]
        public IActionResult Preview()
        {
            var demoPath = Path.Combine(_env.ContentRootPath, "demo");
            if (!Directory.Exists(demoPath))
                return NotFound(new { message = $"'demo' folder not found at: {demoPath}" });

            var chunks = FindChunkDirs(demoPath);
            var preview = chunks.Select(c => new
            {
                folder   = c,
                images   = GetImages(c).Select(Path.GetFileName),
                hasJson  = System.IO.File.Exists(Path.Combine(c, "product.json")),
            });

            return Ok(new
            {
                demoPath,
                totalChunks = chunks.Count,
                chunks      = preview,
            });
        }

        
        [HttpPost("run")]
        public async Task<IActionResult> Run()
        {
            var demoPath = Path.Combine(_env.ContentRootPath, "demo");
            if (!Directory.Exists(demoPath))
                return NotFound(new { message = $"'demo' folder not found at: {demoPath}" });

            var chunks  = FindChunkDirs(demoPath);
            if (chunks.Count == 0)
                return BadRequest(new { message = "No chunk folders found inside 'demo'." });

            var results  = new List<object>();
            int success  = 0;
            int failed   = 0;

            foreach (var chunkDir in chunks)
            {
                var folderName = Path.GetFileName(chunkDir);

                try
                {
                    // ── 1. Read product.json ─────────────────────────────────
                    var jsonPath = Path.Combine(chunkDir, "product.json");
                    if (!System.IO.File.Exists(jsonPath))
                    {
                        _logger.LogWarning($"⚠️  No product.json in {folderName} — skipping.");
                        results.Add(new { folder = folderName, status = "skipped", reason = "missing product.json" });
                        failed++;
                        continue;
                    }

                    var jsonText = await System.IO.File.ReadAllTextAsync(jsonPath);
                    var doc      = JsonDocument.Parse(jsonText).RootElement;

                    // ── 2. Create Product row ────────────────────────────────
                    var product = new Product
                    {
                        Name         = GetString(doc, "product_name"),
                        MiniDesc     = GetString(doc, "mini_description"),
                        Description  = GetString(doc, "description"),
                        CareDetails  = GetString(doc, "care_details"),
                        Price        = GetDecimal(doc, "price"),
                        Discount     = GetDecimal(doc, "discount_percent"),
                        Count        = GetInt(doc, "stock_count"),
                        InjectorUser = GetString(doc, "creator"),
                        CreateTime   = DateTime.UtcNow,
                        UpdateTime   = DateTime.UtcNow,
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();  // need product.Id for relations

                    // ── 3. Category ──────────────────────────────────────────
                    var category = GetString(doc, "category");
                    if (!string.IsNullOrEmpty(category))
                    {
                        _context.ProductCategories.Add(new ProductCategory
                        {
                            ProductId  = product.Id,
                            Type       = category,
                            CreateTime = DateTime.UtcNow,
                            UpdateTime = DateTime.UtcNow,
                        });
                    }

                    // ── 4. Collection ────────────────────────────────────────
                    var collection = GetString(doc, "collection");
                    if (!string.IsNullOrEmpty(collection))
                    {
                        _context.ProductCollections.Add(new ProductCollection
                        {
                            ProductId  = product.Id,
                            Collection = collection,
                            CreateTime = DateTime.UtcNow,
                            UpdateTime = DateTime.UtcNow,
                        });
                    }

                    // ── 5. Colors ────────────────────────────────────────────
                    if (doc.TryGetProperty("available_colors", out var colorsEl))
                    {
                        foreach (var c in colorsEl.EnumerateArray())
                        {
                            var name = c.TryGetProperty("color", out var cn) ? cn.GetString() ?? "" : "";
                            var code = c.TryGetProperty("code",  out var cc) ? cc.GetString() ?? "" : "";
                            if (string.IsNullOrEmpty(name)) continue;

                            _context.ProductColors.Add(new ProductColor
                            {
                                ProductId  = product.Id,
                                ColorName  = name,
                                ColorCode  = code,
                                CreateTime = DateTime.UtcNow,
                                UpdateTime = DateTime.UtcNow,
                            });
                        }
                    }

                    // ── 6. Sizes ─────────────────────────────────────────────
                    if (doc.TryGetProperty("available_sizes", out var sizesEl))
                    {
                        foreach (var s in sizesEl.EnumerateArray())
                        {
                            var size = s.GetString();
                            if (string.IsNullOrEmpty(size)) continue;

                            _context.ProductSizes.Add(new ProductSize
                            {
                                ProductId  = product.Id,
                                Size       = size,
                                CreateTime = DateTime.UtcNow,
                                UpdateTime = DateTime.UtcNow,
                            });
                        }
                    }

                    // ── 7. Upload images to Cloudinary ───────────────────────
                    var imagePaths   = GetImages(chunkDir);
                    var uploadedUrls = new List<string>();

                    foreach (var imgPath in imagePaths)
                    {
                        try
                        {
                            var result = await _cloudinary.UploadImageAsync(
                                imgPath,
                                folder: $"products/{product.Id}"
                            );

                            var url = result.Success ? result.SecureUrl! : imgPath;
                            uploadedUrls.Add(url);

                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId  = product.Id,
                                Image      = url,
                                Type       = "main",
                                CreateTime = DateTime.UtcNow,
                                UpdateTime = DateTime.UtcNow,
                            });

                            _logger.LogInformation($"  ☁️  Uploaded: {Path.GetFileName(imgPath)} → {url}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"  ⚠️  Failed to upload {Path.GetFileName(imgPath)}: {ex.Message}");
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"✅ Seeded product ID {product.Id}: {product.Name}");
                    results.Add(new
                    {
                        folder      = folderName,
                        status      = "success",
                        productId   = product.Id,
                        productName = product.Name,
                        images      = uploadedUrls,
                    });
                    success++;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Failed chunk '{folderName}': {ex.Message}");
                    results.Add(new { folder = folderName, status = "failed", reason = ex.Message });
                    failed++;
                }
            }

            return Ok(new
            {
                message   = $"Seeding complete. ✅ {success} succeeded, ❌ {failed} failed.",
                success,
                failed,
                results,
            });
        }

        // ── DELETE api/seeder/rollback ───────────────────────────────────────
        // Removes ALL products — use only during dev/testing
        [HttpDelete("rollback")]
        public async Task<IActionResult> Rollback()
        {
            var count = await _context.Products.CountAsync();
            _context.Products.RemoveRange(_context.Products);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Rolled back {count} products." });
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// Finds all leaf folders containing a product.json (up to 2 levels deep)
        private List<string> FindChunkDirs(string root)
        {
            var result = new List<string>();

            foreach (var entry in Directory.GetFileSystemEntries(root))
            {
                if (!Directory.Exists(entry)) continue;

                // Direct child has product.json → it's a chunk
                if (System.IO.File.Exists(Path.Combine(entry, "product.json")))
                {
                    result.Add(entry);
                    continue;
                }

                // One level deeper
                foreach (var sub in Directory.GetDirectories(entry))
                {
                    if (System.IO.File.Exists(Path.Combine(sub, "product.json")))
                        result.Add(sub);
                }
            }

            return result.OrderBy(x => x).ToList();
        }

        private static List<string> GetImages(string dir)
        {
            var exts = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
            return Directory.GetFiles(dir)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                .OrderBy(f => f)
                .ToList();
        }

        private static string GetString(JsonElement doc, string key) =>
            doc.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

        private static decimal GetDecimal(JsonElement doc, string key)
        {
            if (!doc.TryGetProperty(key, out var v)) return 0;
            if (v.ValueKind == JsonValueKind.Null)   return 0;
            return v.TryGetDecimal(out var d) ? d : 0;
        }

        private static int GetInt(JsonElement doc, string key) =>
            doc.TryGetProperty(key, out var v) && v.TryGetInt32(out var i) ? i : 0;
    }
}
