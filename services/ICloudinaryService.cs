namespace server
{
    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult> UploadImageAsync(string filePath, string? publicId = null, string? folder = null);
        Task<CloudinaryUploadResult> UploadImageFromStreamAsync(Stream stream, string fileName, string? publicId = null, string? folder = null);
        Task<CloudinaryUploadResult> UploadVideoAsync(string filePath, string? publicId = null, string? folder = null);
        Task<CloudinaryUploadResult> UploadRawFileAsync(string filePath, string? publicId = null, string? folder = null);
        Task<CloudinaryDeleteResult> DeleteAsync(string publicId, string resourceType = "image");
        string GetTransformedUrl(string publicId, CloudinaryTransformOptions options);
        string GetSignedUrl(string publicId, int expiresInSeconds = 3600);
    }
}