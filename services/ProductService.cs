using Microsoft.EntityFrameworkCore;
using Sql.Models;
using Sql.Utils;
using Eservice.RedisServices;

namespace Sql.Services
{
    public class ProductService : IProductService
    {
        private readonly ProductContext _context;
        private readonly server.ICloudinaryService _cloudinary;
        private readonly IRedisRepository _redis;
        private readonly ILogger<ProductService> _logger;

        private const string ProductKeyPrefix    = "product:";
        private const string IndexKey            = "products:index";
        private const string ReviewsPrefix       = "product:reviews:";
        private const string AllProductsKey      = "products:all";
        private const string TrendingProductsKey = "products:trending";

        // 🔥 Only the newest 50 products are kept eagerly cached in Redis
        private const int CacheLimit = 50;

        private static readonly TimeSpan? ProductTtl = null; // no expiry — write-through cache
        private static readonly TimeSpan ReviewsTtl = TimeSpan.FromMinutes(30);

        public ProductService(
            ProductContext context,
            server.ICloudinaryService cloudinary,
            IRedisRepository redis,
            ILogger<ProductService> logger)
        {
            _context = context;
            _cloudinary = cloudinary;
            _redis = redis;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        // 🔥 CACHE WARM-UP — only loads the latest 50 products into Redis
        // ════════════════════════════════════════════════════════════════════
        public async Task<int> LoadAllProductsToRedisAsync()
        {
            var products = await _context.Products
                .Include(p => p.Categories).Include(p => p.Collections)
                .Include(p => p.Colors).Include(p => p.Sizes)
                .Include(p => p.Images).Include(p => p.Reviews)
                .OrderByDescending(p => p.CreateTime)
                .Take(CacheLimit) // 🔥 cap at 50, not 600+
                .ToListAsync();

            // Clear old index/cache first so stale encoded keys don't linger
            var oldIds = await _redis.SortedSetRangeByRankAsync(IndexKey, 0, -1);
            if (oldIds.Count > 0)
            {
                await _redis.RemoveKeysAsync(oldIds.Select(eid => $"{ProductKeyPrefix}{eid}"));
            }
            await _redis.RemoveKeyAsync(IndexKey);
            await _redis.RemoveKeyAsync(AllProductsKey);

            int count = 0;
            var responses = new List<ProductResponse>();

            foreach (var product in products)
            {
                var encodedId = IdEncoder.Encode(product.Id);
                var response  = MapToResponse(product);

                await _redis.SetObjectAsync($"{ProductKeyPrefix}{encodedId}", response, ProductTtl);
                await _redis.SortedSetAddAsync(IndexKey, encodedId, product.CreateTime.Ticks);

                responses.Add(response);
                count++;
            }

            var ordered = responses.OrderByDescending(p => p.CreateTime).ToList();
            await _redis.SetObjectAsync(AllProductsKey, ordered, ProductTtl);

            // 🔥 Pick 3 random products as "trending" from the cached 50
            await RefreshTrendingProductsCacheAsync(responses);

            _logger.LogInformation($"✅ Loaded {count} (of top {CacheLimit}) products into Redis cache");

            return count;
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
        {
            var product = new Product
            {
                Name = request.Name, Count = request.Count, MiniDesc = request.MiniDesc,
                Description = request.Description, CareDetails = request.CareDetails,
                Price = request.Price, Discount = request.Discount, InjectorUser = request.InjectorUser,
                CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Category))
                _context.ProductCategories.Add(new ProductCategory { ProductId = product.Id, Type = request.Category, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow });

            if (!string.IsNullOrEmpty(request.Collection))
                _context.ProductCollections.Add(new ProductCollection { ProductId = product.Id, Collection = request.Collection, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow });

            if (request.Colors?.Count > 0)
                _context.ProductColors.AddRange(request.Colors.Select(c => new ProductColor
                {
                    ProductId = product.Id, ColorName = c.ColorName, ColorCode = c.ColorCode,
                    CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
                }));

            if (request.Sizes?.Count > 0)
                _context.ProductSizes.AddRange(request.Sizes.Select(s => new ProductSize
                {
                    ProductId = product.Id, Size = s, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
                }));

            if (request.Images?.Count > 0)
            {
                var imageEntities = new List<ProductImage>();
                foreach (var img in request.Images)
                {
                    var imageUrl = await UploadImageAndGetUrlAsync(img.Image, folder: $"products/{product.Id}");
                    imageEntities.Add(new ProductImage
                    {
                        ProductId = product.Id, Image = imageUrl, Type = img.Type,
                        CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
                    });
                }
                _context.ProductImages.AddRange(imageEntities);
            }

            await _context.SaveChangesAsync();

            var fullProduct = await LoadFullProductAsync(product.Id);
            var response = MapToResponse(fullProduct!);

            // ── Redis: add new product into cache + index + blob (auto-trims to 50) ──
            await UpsertProductInCacheAsync(response);

            return response;
        }

        // ── UPDATE (SQL + Redis both updated) ────────────────────────────────
        public async Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request)
        {
            var product = await LoadFullProductAsync(id);
            if (product == null) return null;

            if (!string.IsNullOrEmpty(request.Name)) product.Name = request.Name;
            if (request.Count.HasValue) product.Count = request.Count.Value;
            if (!string.IsNullOrEmpty(request.MiniDesc)) product.MiniDesc = request.MiniDesc;
            if (!string.IsNullOrEmpty(request.Description)) product.Description = request.Description;
            if (!string.IsNullOrEmpty(request.CareDetails)) product.CareDetails = request.CareDetails;
            if (request.Price.HasValue) product.Price = request.Price.Value;
            if (request.Discount.HasValue) product.Discount = request.Discount.Value;
            if (!string.IsNullOrEmpty(request.InjectorUser)) product.InjectorUser = request.InjectorUser;
            product.UpdateTime = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Category))
            {
                var existing = await _context.ProductCategories.FirstOrDefaultAsync(c => c.ProductId == id);
                if (existing != null) { existing.Type = request.Category; existing.UpdateTime = DateTime.UtcNow; }
                else _context.ProductCategories.Add(new ProductCategory { ProductId = id, Type = request.Category, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow });
            }

            if (!string.IsNullOrEmpty(request.Collection))
            {
                var existing = await _context.ProductCollections.FirstOrDefaultAsync(c => c.ProductId == id);
                if (existing != null) { existing.Collection = request.Collection; existing.UpdateTime = DateTime.UtcNow; }
                else _context.ProductCollections.Add(new ProductCollection { ProductId = id, Collection = request.Collection, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow });
            }

            if (request.Colors != null)
            {
                _context.ProductColors.RemoveRange(await _context.ProductColors.Where(c => c.ProductId == id).ToListAsync());
                _context.ProductColors.AddRange(request.Colors.Select(c => new ProductColor
                {
                    ProductId = id, ColorName = c.ColorName, ColorCode = c.ColorCode,
                    CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
                }));
            }

            if (request.Sizes != null)
            {
                _context.ProductSizes.RemoveRange(await _context.ProductSizes.Where(s => s.ProductId == id).ToListAsync());
                _context.ProductSizes.AddRange(request.Sizes.Select(s => new ProductSize
                {
                    ProductId = id, Size = s, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
                }));
            }

            if (request.Images != null)
            {
                var existingImages = await _context.ProductImages.Where(i => i.ProductId == id).ToListAsync();

                foreach (var old in existingImages)
                {
                    if (old.Image.Contains("cloudinary.com"))
                    {
                        var uri = new Uri(old.Image);
                        var segments = uri.AbsolutePath.Split('/');
                        var uploadIndex = Array.IndexOf(segments, "upload");
                        if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
                        {
                            var publicIdWithExt = string.Join("/", segments[(uploadIndex + 1)..]);
                            var publicId = Path.GetFileNameWithoutExtension(publicIdWithExt);
                            var folder = Path.GetDirectoryName(publicIdWithExt)?.Replace("\\", "/");
                            var fullPublicId = string.IsNullOrEmpty(folder) ? publicId : $"{folder}/{publicId}";
                            await _cloudinary.DeleteAsync(fullPublicId);
                        }
                    }
                }

                _context.ProductImages.RemoveRange(existingImages);

                var newImages = new List<ProductImage>();
                foreach (var img in request.Images)
                {
                    var imageUrl = await UploadImageAndGetUrlAsync(img.Image, folder: $"products/{id}");
                    newImages.Add(new ProductImage
                    {
                        ProductId = id, Image = imageUrl, Type = img.Type,
                        CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
                    });
                }
                _context.ProductImages.AddRange(newImages);
            }

            await _context.SaveChangesAsync();

            var updated = await LoadFullProductAsync(id);
            if (updated == null) return null;

            var response = MapToResponse(updated);

            // ── Redis: overwrite cached product + refresh blob (trims to 50) ──
            await UpsertProductInCacheAsync(response);

            return response;
        }

        // ── DELETE (SQL + Redis both updated) ────────────────────────────────
        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            var encodedId = IdEncoder.Encode(id);
            await _redis.RemoveKeyAsync($"{ProductKeyPrefix}{encodedId}");
            await _redis.RemoveKeyAsync($"{ReviewsPrefix}{encodedId}");
            await _redis.SortedSetRemoveAsync(IndexKey, encodedId);

            // 🔥 keep the blob (and trending) in sync
            await RefreshAllProductsCacheAsync();

            return true;
        }

        // ════════════════════════════════════════════════════════════════════
        // READS — Redis first, auto-fallback to SQL (and re-cache) on miss
        // ════════════════════════════════════════════════════════════════════

        public async Task<ProductResponse?> GetProductByIdAsync(int id)
        {
            var encodedId = IdEncoder.Encode(id);
            var cached = await _redis.GetObjectAsync<ProductResponse>($"{ProductKeyPrefix}{encodedId}");
            if (cached != null) return cached;

            // 🔥 Not in the cached top-50 — pull from SQL and cache it for next time
            _logger.LogInformation($"ℹ️ Cache miss for product id={id} — fetching from SQL");
            var product = await LoadFullProductAsync(id);
            if (product == null) return null;

            var response = MapToResponse(product);
            await CacheSingleProductAsync(encodedId, response);

            return response;
        }

        public async Task<ProductResponse?> GetProductByEncodedIdAsync(string encodedId)
        {
            var cached = await _redis.GetObjectAsync<ProductResponse>($"{ProductKeyPrefix}{encodedId}");
            if (cached != null) return cached;

            // 🔥 Decode back to numeric id so we can hit SQL directly
            var id = IdEncoder.Decode(encodedId);
            if (id == null)
            {
                _logger.LogWarning($"⚠️ Could not decode encodedId '{encodedId}'");
                return null;
            }

            _logger.LogInformation($"ℹ️ Cache miss for product encodedId={encodedId} — fetching from SQL");
            var product = await LoadFullProductAsync(id.Value);
            if (product == null) return null;

            var response = MapToResponse(product);
            await CacheSingleProductAsync(encodedId, response);

            return response;
        }

        public async Task<PaginatedResponse<ProductResponse>> GetAllProductsAsync(int page = 1, int pageSize = 12)
        {
            var cachedProducts = await GetAllProductsCachedAsync(); // top 50 only
            var requiredCount = page * pageSize;

            if (requiredCount <= cachedProducts.Count)
            {
                // Fully servable from the cached top-50
                var paged = cachedProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var totalInDb = await _context.Products.CountAsync();

                return new PaginatedResponse<ProductResponse>
                {
                    Data = paged, Total = totalInDb, Page = page, PageSize = pageSize,
                };
            }

            // 🔥 Requested page goes beyond the cached window — go straight to SQL
            _logger.LogInformation($"ℹ️ Page {page} exceeds cached window — querying SQL directly");

            var dbProducts = await _context.Products
                .Include(p => p.Categories).Include(p => p.Collections)
                .Include(p => p.Colors).Include(p => p.Sizes)
                .Include(p => p.Images).Include(p => p.Reviews)
                .OrderByDescending(p => p.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Products.CountAsync();
            var data = dbProducts.Select(MapToResponse).ToList();

            return new PaginatedResponse<ProductResponse>
            {
                Data = data, Total = total, Page = page, PageSize = pageSize,
            };
        }

        public async Task<PaginatedResponse<ProductResponse>> SearchProductsAsync(string query, int page = 1, int pageSize = 12)
        {
            var q = query.ToLower();

            var cachedProducts = await GetAllProductsCachedAsync(); // top 50 only

            var filteredCached = cachedProducts.Where(p =>
                (p.Name?.ToLower().Contains(q) ?? false) ||
                (p.Description?.ToLower().Contains(q) ?? false) ||
                (p.MiniDesc?.ToLower().Contains(q) ?? false) ||
                (p.Category?.ToLower().Contains(q) ?? false) ||
                (p.Collection?.ToLower().Contains(q) ?? false)
            )
            .OrderByDescending(p => p.Name?.ToLower().StartsWith(q) ?? false)
            .ThenByDescending(p => p.CreateTime)
            .ToList();

            var requiredCount = page * pageSize;

            // 🔥 If the cached top-50 already gives us enough matches for this page, use it.
            // Otherwise fall back to a real SQL search across the whole catalog.
            if (requiredCount <= filteredCached.Count)
            {
                var paged = filteredCached.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return new PaginatedResponse<ProductResponse>
                {
                    Data = paged, Total = filteredCached.Count, Page = page, PageSize = pageSize,
                };
            }

            _logger.LogInformation($"ℹ️ Search '{query}' needs more results than cache has — querying SQL directly");

            var dbQuery = _context.Products
                .Include(p => p.Categories).Include(p => p.Collections)
                .Include(p => p.Colors).Include(p => p.Sizes)
                .Include(p => p.Images).Include(p => p.Reviews)
                .Where(p =>
                    p.Name.ToLower().Contains(q) ||
                    (p.Description != null && p.Description.ToLower().Contains(q)) ||
                    (p.MiniDesc != null && p.MiniDesc.ToLower().Contains(q)) ||
                    p.Categories.Any(c => c.Type.ToLower().Contains(q)) ||
                    p.Collections.Any(c => c.Collection.ToLower().Contains(q))
                );

            var total = await dbQuery.CountAsync();

            var dbResults = await dbQuery
                .OrderByDescending(p => p.Name.ToLower().StartsWith(q))
                .ThenByDescending(p => p.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = dbResults.Select(MapToResponse).ToList();

            return new PaginatedResponse<ProductResponse>
            {
                Data = data, Total = total, Page = page, PageSize = pageSize,
            };
        }

        public async Task<PaginatedResponse<ProductResponse>> GetProductsByCategoryAsync(string category, int page = 1, int pageSize = 12)
        {
            var cachedProducts = await GetAllProductsCachedAsync(); // top 50 only

            var filteredCached = cachedProducts
                .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreateTime)
                .ToList();

            var requiredCount = page * pageSize;

            if (requiredCount <= filteredCached.Count)
            {
                var paged = filteredCached.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return new PaginatedResponse<ProductResponse>
                {
                    Data = paged, Total = filteredCached.Count, Page = page, PageSize = pageSize,
                };
            }

            _logger.LogInformation($"ℹ️ Category '{category}' page {page} exceeds cache — querying SQL directly");

            var dbQuery = _context.Products
                .Include(p => p.Categories).Include(p => p.Collections)
                .Include(p => p.Colors).Include(p => p.Sizes)
                .Include(p => p.Images).Include(p => p.Reviews)
                .Where(p => p.Categories.Any(c => c.Type.ToLower() == category.ToLower()));

            var total = await dbQuery.CountAsync();

            var dbResults = await dbQuery
                .OrderByDescending(p => p.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = dbResults.Select(MapToResponse).ToList();

            return new PaginatedResponse<ProductResponse>
            {
                Data = data, Total = total, Page = page, PageSize = pageSize,
            };
        }

        public async Task<PaginatedResponse<ProductResponse>> GetProductsByCollectionAsync(string collection, int page = 1, int pageSize = 12)
        {
            var cachedProducts = await GetAllProductsCachedAsync(); // top 50 only

            var filteredCached = cachedProducts
                .Where(p => string.Equals(p.Collection, collection, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreateTime)
                .ToList();

            var requiredCount = page * pageSize;

            if (requiredCount <= filteredCached.Count)
            {
                var paged = filteredCached.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return new PaginatedResponse<ProductResponse>
                {
                    Data = paged, Total = filteredCached.Count, Page = page, PageSize = pageSize,
                };
            }

            _logger.LogInformation($"ℹ️ Collection '{collection}' page {page} exceeds cache — querying SQL directly");

            var dbQuery = _context.Products
                .Include(p => p.Categories).Include(p => p.Collections)
                .Include(p => p.Colors).Include(p => p.Sizes)
                .Include(p => p.Images).Include(p => p.Reviews)
                .Where(p => p.Collections.Any(c => c.Collection.ToLower() == collection.ToLower()));

            var total = await dbQuery.CountAsync();

            var dbResults = await dbQuery
                .OrderByDescending(p => p.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = dbResults.Select(MapToResponse).ToList();

            return new PaginatedResponse<ProductResponse>
            {
                Data = data, Total = total, Page = page, PageSize = pageSize,
            };
        }

        // ── TRENDING (Redis only, refreshed on cache warm-up / rebuild) ─────
        public async Task<List<ProductResponse>> GetTrendingProductsAsync()
        {
            var cached = await _redis.GetObjectAsync<List<ProductResponse>>(TrendingProductsKey);
            if (cached != null && cached.Count > 0) return cached;

            // Fallback: rebuild trending from whatever is currently cached
            var allProducts = await GetAllProductsCachedAsync();
            if (allProducts.Count == 0)
            {
                // 🔥 Nothing cached at all — self-heal by warming the cache from SQL
                _logger.LogWarning("⚠️ Trending + cache both empty — auto re-warming from SQL");
                await LoadAllProductsToRedisAsync();
                allProducts = await GetAllProductsCachedAsync();
                if (allProducts.Count == 0) return new List<ProductResponse>();
            }

            await RefreshTrendingProductsCacheAsync(allProducts);
            return await _redis.GetObjectAsync<List<ProductResponse>>(TrendingProductsKey) ?? new List<ProductResponse>();
        }

        // ── REVIEWS (cache-aside, keyed by encoded id) ───────────────────────
        public async Task<List<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            var encodedId = IdEncoder.Encode(productId);
            var cacheKey  = $"{ReviewsPrefix}{encodedId}";

            var cached = await _redis.GetObjectAsync<List<ReviewDto>>(cacheKey);
            if (cached != null) return cached;

            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreateTime).ToListAsync();

            var result = reviews.Select(r => new ReviewDto
            {
                Id = r.Id, Username = r.Username, Rating = r.Rating,
                Comment = r.Comment, React = r.React, CreateTime = r.CreateTime,
            }).ToList();

            await _redis.SetObjectAsync(cacheKey, result, ReviewsTtl);
            return result;
        }

        public async Task<ReviewDto> AddReviewAsync(int productId, ReviewDto reviewDto)
        {
            var review = new Review
            {
                ProductId = productId, Username = reviewDto.Username, Rating = reviewDto.Rating,
                Comment = reviewDto.Comment, React = 0,
                CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var result = new ReviewDto
            {
                Id = review.Id, Username = review.Username, Rating = review.Rating,
                Comment = review.Comment, React = review.React, CreateTime = review.CreateTime,
            };

            var encodedId = IdEncoder.Encode(productId);
            await _redis.RemoveKeyAsync($"{ReviewsPrefix}{encodedId}");

            // Refresh the cached product so AverageRating/ReviewCount stay accurate
            var updatedProduct = await LoadFullProductAsync(productId);
            if (updatedProduct != null)
                await UpsertProductInCacheAsync(MapToResponse(updatedProduct));

            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the cached top-50 product list from ONE Redis call.
        /// Falls back to rebuilding from per-product keys, then to SQL if Redis is completely cold.
        /// </summary>
        private async Task<List<ProductResponse>> GetAllProductsCachedAsync()
        {
            var cached = await _redis.GetObjectAsync<List<ProductResponse>>(AllProductsKey);
            if (cached != null) return cached;

            // Cache miss/expired -> rebuild once from per-product keys, then cache it.
            await RefreshAllProductsCacheAsync();
            var rebuilt = await _redis.GetObjectAsync<List<ProductResponse>>(AllProductsKey);
            if (rebuilt != null && rebuilt.Count > 0) return rebuilt;

            // 🔥 Last-resort: Redis is completely empty (restart/flush) — re-warm straight from SQL
            _logger.LogWarning("⚠️ Product cache completely empty — re-warming from SQL");
            await LoadAllProductsToRedisAsync();
            return await _redis.GetObjectAsync<List<ProductResponse>>(AllProductsKey) ?? new List<ProductResponse>();
        }

        /// <summary>
        /// Rebuilds the "products:all" blob from the sorted-set index + per-product keys,
        /// trimming down to the newest CacheLimit (50) if it has grown past that.
        /// Also refreshes trending picks so they stay in sync.
        /// </summary>
        private async Task RefreshAllProductsCacheAsync()
        {
            var allIds = await _redis.SortedSetRangeByRankAsync(IndexKey, 0, -1, descending: true);

            // 🔥 Trim anything beyond the top 50 out of Redis entirely
            if (allIds.Count > CacheLimit)
            {
                var overflow = allIds.Skip(CacheLimit).ToList();

                await _redis.RemoveKeysAsync(overflow.Select(eid => $"{ProductKeyPrefix}{eid}"));
                foreach (var eid in overflow)
                    await _redis.SortedSetRemoveAsync(IndexKey, eid);

                allIds = allIds.Take(CacheLimit).ToList();
            }

            var products = new List<ProductResponse>();
            foreach (var eid in allIds)
            {
                var p = await _redis.GetObjectAsync<ProductResponse>($"{ProductKeyPrefix}{eid}");
                if (p != null) products.Add(p);
            }

            await _redis.SetObjectAsync(AllProductsKey, products, ProductTtl);

            // 🔥 keep trending in sync too
            await RefreshTrendingProductsCacheAsync(products);
        }

        /// <summary>
        /// Picks 3 random products from the given list and caches them under TrendingProductsKey.
        /// </summary>
        private async Task RefreshTrendingProductsCacheAsync(List<ProductResponse> source)
        {
            if (source == null || source.Count == 0)
            {
                await _redis.RemoveKeyAsync(TrendingProductsKey);
                return;
            }

            var trending = source
                .OrderBy(_ => Random.Shared.Next())
                .Take(3)
                .ToList();

            await _redis.SetObjectAsync(TrendingProductsKey, trending, ProductTtl);
        }

        /// <summary>
        /// Caches a single product that was fetched on a cache-miss (auto-heal path),
        /// then re-trims the overall cache back down to CacheLimit.
        /// </summary>
        private async Task CacheSingleProductAsync(string encodedId, ProductResponse response)
        {
            await _redis.SetObjectAsync($"{ProductKeyPrefix}{encodedId}", response, ProductTtl);
            await _redis.SortedSetAddAsync(IndexKey, encodedId, response.CreateTime.Ticks);

            // 🔥 Re-trim to CacheLimit in case this push made it 51
            await RefreshAllProductsCacheAsync();
        }

        private async Task UpsertProductInCacheAsync(ProductResponse response)
        {
            var encodedId = IdEncoder.Encode(response.Id);
            await _redis.SetObjectAsync($"{ProductKeyPrefix}{encodedId}", response, ProductTtl);
            await _redis.SortedSetAddAsync(IndexKey, encodedId, response.CreateTime.Ticks);

            // 🔥 keep the single blob (and trending) in sync, trimmed to CacheLimit
            await RefreshAllProductsCacheAsync();
        }

        private async Task<Product?> LoadFullProductAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Categories).Include(p => p.Collections)
                .Include(p => p.Colors).Include(p => p.Sizes)
                .Include(p => p.Images).Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task<string> UploadImageAndGetUrlAsync(string imageData, string folder = "products")
        {
            if (imageData.StartsWith("http://") || imageData.StartsWith("https://")) return imageData;

            if (imageData.StartsWith("data:image") || IsBase64(imageData))
            {
                var base64Data = imageData.Contains(",") ? imageData.Split(',')[1] : imageData;
                var bytes = Convert.FromBase64String(base64Data);
                using var stream = new MemoryStream(bytes);
                var result = await _cloudinary.UploadImageFromStreamAsync(stream, "product.jpg", folder: folder);
                return result.Success ? result.SecureUrl! : imageData;
            }

            if (File.Exists(imageData))
            {
                var result = await _cloudinary.UploadImageAsync(imageData, folder: folder);
                return result.Success ? result.SecureUrl! : imageData;
            }

            return imageData;
        }

        private static bool IsBase64(string s)
        {
            try { Convert.FromBase64String(s); return s.Length % 4 == 0; }
            catch { return false; }
        }

        private ProductResponse MapToResponse(Product product)
        {
            return new ProductResponse
            {
                Id           = product.Id,
                EncodedId    = IdEncoder.Encode(product.Id),
                Name         = product.Name,
                Count        = product.Count,
                MiniDesc     = product.MiniDesc,
                Description  = product.Description,
                CareDetails  = product.CareDetails,
                Price        = product.Price,
                Discount     = product.Discount,
                InjectorUser = product.InjectorUser,
                Category     = product.Categories?.FirstOrDefault()?.Type,
                Collection   = product.Collections?.FirstOrDefault()?.Collection,
                CreateTime   = product.CreateTime,
                UpdateTime   = product.UpdateTime,
                Colors = product.Colors?.Select(c => new ColorDto
                {
                    Id = c.Id, ColorName = c.ColorName, ColorCode = c.ColorCode,
                }).ToList() ?? new(),
                Sizes  = product.Sizes?.Select(s => s.Size).ToList() ?? new(),
                Images = product.Images?.Select(i => new ImageDto
                {
                    Id = i.Id, Image = i.Image, Type = i.Type,
                }).ToList() ?? new(),
                AverageRating = product.Reviews?.Any() == true ? product.Reviews.Average(r => r.Rating) : 0,
                ReviewCount   = product.Reviews?.Count ?? 0,
            };
        }
    }
}