namespace server
{
    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string? PublicId { get; set; }
        public string? SecureUrl { get; set; }
        public string? Format { get; set; }
        public long Bytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? ResourceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
 
    public class CloudinaryDeleteResult
    {
        public bool Success { get; set; }
        public string? PublicId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
 
    public class CloudinaryTransformOptions
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Crop { get; set; } = "fill";
        public string? Gravity { get; set; }
        public string? Format { get; set; }
        public int? Quality { get; set; }
    }
}
 