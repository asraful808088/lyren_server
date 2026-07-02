using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace server
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"]
                ?? throw new InvalidOperationException("Cloudinary:CloudName is not configured.");
            var apiKey = configuration["Cloudinary:ApiKey"]
                ?? throw new InvalidOperationException("Cloudinary:ApiKey is not configured.");
            var apiSecret = configuration["Cloudinary:ApiSecret"]
                ?? throw new InvalidOperationException("Cloudinary:ApiSecret is not configured.");

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        // ── Upload Image from File Path ──────────────────────────────────────────
        public async Task<CloudinaryUploadResult> UploadImageAsync(
            string filePath, string? publicId = null, string? folder = null)
        {
            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File           = new FileDescription(filePath),
                    PublicId       = publicId,
                    Folder         = folder,
                    Overwrite      = true,
                    UniqueFilename = publicId == null
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                {
                    _logger.LogError("Upload error: {Error}", result.Error.Message);
                    return new CloudinaryUploadResult { Success = false, ErrorMessage = result.Error.Message };
                }
                _logger.LogInformation("Image uploaded: {PublicId}", result.PublicId);
                return MapUploadResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception uploading: {FilePath}", filePath);
                return new CloudinaryUploadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CloudinaryUploadResult> UploadImageFromStreamAsync(
            Stream stream, string fileName, string? publicId = null, string? folder = null)
        {
            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File           = new FileDescription(fileName, stream),
                    PublicId       = publicId,
                    Folder         = folder,
                    Overwrite      = true,
                    UniqueFilename = publicId == null
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                {
                    _logger.LogError("Stream upload error: {Error}", result.Error.Message);
                    return new CloudinaryUploadResult { Success = false, ErrorMessage = result.Error.Message };
                }
                _logger.LogInformation("Stream uploaded: {PublicId}", result.PublicId);
                return MapUploadResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception uploading stream: {FileName}", fileName);
                return new CloudinaryUploadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // ── Upload Video ─────────────────────────────────────────────────────────
        public async Task<CloudinaryUploadResult> UploadVideoAsync(
            string filePath, string? publicId = null, string? folder = null)
        {
            try
            {
                var uploadParams = new VideoUploadParams
                {
                    File      = new FileDescription(filePath),
                    PublicId  = publicId,
                    Folder    = folder,
                    Overwrite = true
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                {
                    _logger.LogError("Video upload error: {Error}", result.Error.Message);
                    return new CloudinaryUploadResult { Success = false, ErrorMessage = result.Error.Message };
                }
                _logger.LogInformation("Video uploaded: {PublicId}", result.PublicId);
                return MapUploadResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception uploading video: {FilePath}", filePath);
                return new CloudinaryUploadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // ── Upload Raw File ──────────────────────────────────────────────────────
        public async Task<CloudinaryUploadResult> UploadRawFileAsync(
            string filePath, string? publicId = null, string? folder = null)
        {
            try
            {
                var uploadParams = new RawUploadParams
                {
                    File      = new FileDescription(filePath),
                    PublicId  = publicId,
                    Folder    = folder,
                    Overwrite = true
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                {
                    _logger.LogError("Raw upload error: {Error}", result.Error.Message);
                    return new CloudinaryUploadResult { Success = false, ErrorMessage = result.Error.Message };
                }
                _logger.LogInformation("Raw file uploaded: {PublicId}", result.PublicId);
                return MapUploadResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception uploading raw: {FilePath}", filePath);
                return new CloudinaryUploadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // ── Delete ───────────────────────────────────────────────────────────────
        public async Task<CloudinaryDeleteResult> DeleteAsync(string publicId, string resourceType = "image")
        {
            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = resourceType switch
                    {
                        "video" => ResourceType.Video,
                        "raw"   => ResourceType.Raw,
                        _       => ResourceType.Image
                    }
                };
                var result = await _cloudinary.DestroyAsync(deleteParams);
                if (result.Error != null)
                {
                    _logger.LogError("Delete error: {Error}", result.Error.Message);
                    return new CloudinaryDeleteResult { Success = false, PublicId = publicId, ErrorMessage = result.Error.Message };
                }
                _logger.LogInformation("Deleted: {PublicId}", publicId);
                return new CloudinaryDeleteResult { Success = result.Result == "ok", PublicId = publicId, Status = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting: {PublicId}", publicId);
                return new CloudinaryDeleteResult { Success = false, PublicId = publicId, ErrorMessage = ex.Message };
            }
        }

        // ── Get Transformed URL ──────────────────────────────────────────────────
        public string GetTransformedUrl(string publicId, CloudinaryTransformOptions options)
        {
            var t = new Transformation();
            if (options.Width.HasValue)                 t = t.Width(options.Width.Value);
            if (options.Height.HasValue)                t = t.Height(options.Height.Value);
            if (!string.IsNullOrEmpty(options.Crop))    t = t.Crop(options.Crop);
            if (!string.IsNullOrEmpty(options.Gravity)) t = t.Gravity(options.Gravity);
            if (!string.IsNullOrEmpty(options.Format))  t = t.FetchFormat(options.Format);
            if (options.Quality.HasValue)               t = t.Quality(options.Quality.Value);

            return _cloudinary.Api.UrlImgUp.Transform(t).Secure(true).BuildUrl(publicId);
        }

        // ── Get Signed URL ───────────────────────────────────────────────────────
        public string GetSignedUrl(string publicId, int expiresInSeconds = 3600)
        {
            var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresInSeconds;
            return _cloudinary.Api.UrlImgUp.Secure(true).Signed(true).BuildUrl($"{publicId}?_expires={expiresAt}");
        }

        // ── Helper ───────────────────────────────────────────────────────────────
        private static CloudinaryUploadResult MapUploadResult(RawUploadResult result)
        {
            return new CloudinaryUploadResult
            {
                Success      = true,
                PublicId     = result.PublicId,
                SecureUrl    = result.SecureUrl?.ToString(),
                Format       = result.Format,
                Bytes        = result.Bytes,
                Width        = result is ImageUploadResult img  ? img.Width  : 0,
                Height       = result is ImageUploadResult img2 ? img2.Height : 0,
                ResourceType = result.ResourceType,
                CreatedAt    = result.CreatedAt
            };
        }
    }
}