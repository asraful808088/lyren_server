using System;
using System.Collections.Generic;

namespace Sql.Models
{
  
    public class AlgoliaProduct
    {
        public string ObjectID { get; set; } = null!;  
        public int RawId { get; set; }               
        public string Name { get; set; } = "";
        public string MiniDesc { get; set; } = "";
        public string Description { get; set; } = "";
        public string CareDetails { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int Count { get; set; }
        public string? Category { get; set; }
        public string? Collection { get; set; }
        public string? ThumbnailUrl { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<string> Sizes { get; set; } = new();
        public List<string> Colors { get; set; } = new();
        public long CreatedAt { get; set; }      // Unix timestamp
        public long UpdatedAt { get; set; }      // Unix timestamp
    }

    /// <summary>
    /// Search result wrapper from Algolia.
    /// </summary>
    public class AlgoliaSearchResult
    {
        public List<AlgoliaProduct> Hits { get; set; } = new();
        public int TotalHits { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string Query { get; set; } = "";
    }

    // ════════════════════════════════════════════════════════════════════════
    // API RESPONSE WRAPPER
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Standard API response format for all endpoints.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    // REQUEST/RESPONSE DTOs
    // ════════════════════════════════════════════════════════════════════════

    public class CreateProductRequest
    {
        public string Name { get; set; } = null!;
        public string? MiniDesc { get; set; }
        public string? Description { get; set; }
        public string? CareDetails { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int Count { get; set; }
        public List<string>? CategoryIds { get; set; }
        public List<string>? CollectionIds { get; set; }
        public List<string>? Sizes { get; set; }
        public List<string>? Colors { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? MiniDesc { get; set; }
        public string? Description { get; set; }
        public string? CareDetails { get; set; }
        public decimal? Price { get; set; }
        public decimal? Discount { get; set; }
        public int? Count { get; set; }
        public List<string>? CategoryIds { get; set; }
        public List<string>? CollectionIds { get; set; }
        public List<string>? Sizes { get; set; }
        public List<string>? Colors { get; set; }
    }

    public class ProductResponse
    {
        public int Id { get; set; }
        public string EncodedId { get; set; } = null!;  // Added by controller
        public string Name { get; set; } = null!;
        public string? MiniDesc { get; set; }
        public string? Description { get; set; }
        public string? CareDetails { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int Count { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}