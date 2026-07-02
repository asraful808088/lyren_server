namespace Sql.Services
{
    // ========================================
    // CREATE PRODUCT REQUEST
    // ========================================
    public class CreateProductRequest
    {
        public required string Name { get; set; }
        public int Count { get; set; }
        public required string MiniDesc { get; set; }
        public required string Description { get; set; }
        public string? CareDetails { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public required string InjectorUser { get; set; }
        public string? Category { get; set; }
        public string? Collection { get; set; }
        public List<ColorDto>? Colors { get; set; }
        public List<string>? Sizes { get; set; }
        public List<ImageDto>? Images { get; set; }
    }

    // ========================================
    // UPDATE PRODUCT REQUEST
    // ========================================
    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public int? Count { get; set; }
        public string? MiniDesc { get; set; }
        public string? Description { get; set; }
        public string? CareDetails { get; set; }
        public decimal? Price { get; set; }
        public decimal? Discount { get; set; }
        public string? InjectorUser { get; set; }
        public string? Category { get; set; }
        public string? Collection { get; set; }
        public List<ColorDto>? Colors { get; set; }
        public List<string>? Sizes { get; set; }
        public List<ImageDto>? Images { get; set; }
    }

    // ========================================
    // PRODUCT RESPONSE
    // ========================================
    public class ProductResponse
{
    public int Id { get; set; }
    public string EncodedId { get; set; }  // ← Add this line
    public required string Name { get; set; }
    public int Count { get; set; }
    public required string MiniDesc { get; set; }
    public required string Description { get; set; }
    public string? CareDetails { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public required string InjectorUser { get; set; }
    public string? Category { get; set; }
    public string? Collection { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public List<ColorDto> Colors { get; set; } = new();
    public List<string> Sizes { get; set; } = new();
    public List<ImageDto> Images { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
    // ========================================
    // HELPER DTOs
    // ========================================
    public class ColorDto
    {
        public int? Id { get; set; }
        public required string ColorName { get; set; }
        public required string ColorCode { get; set; }
    }

    public class ImageDto
    {
        public int? Id { get; set; }
        public required string Image { get; set; }
        public required string Type { get; set; } // "main", "gallery", "thumbnail"
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int React { get; set; }
        public DateTime CreateTime { get; set; }
    }

    

    // ========================================
    // PAGINATED RESPONSE
    // ========================================
    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }

    // ========================================
    // API RESPONSE WRAPPER
    // ========================================
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public T? Data { get; set; }
    }
}
