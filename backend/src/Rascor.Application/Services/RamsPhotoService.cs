using Microsoft.Extensions.Logging;
using Rascor.Domain;
using Rascor.Domain.Entities;

namespace Rascor.Application.Services;

public class RamsPhotoService
{
    private readonly IRamsPhotoRepository _repository;
    private readonly ILogger<RamsPhotoService> _logger;
    private readonly string _uploadDirectory;

    public RamsPhotoService(
        IRamsPhotoRepository repository,
        ILogger<RamsPhotoService> logger,
        string uploadDirectory)
    {
        _repository = repository;
        _logger = logger;
        _uploadDirectory = uploadDirectory;

        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<RamsPhotoUploadResult> UploadPhotoAsync(
        string userId,
        string siteId,
        DateTime capturedAt,
        Stream photoStream,
        string originalFilename,
        long fileSize)
    {
        try
        {
            // Validate file size (max 10MB)
            if (fileSize > 10 * 1024 * 1024)
            {
                return new RamsPhotoUploadResult
                {
                    Success = false,
                    Message = "File too large. Maximum size is 10MB."
                };
            }

            // Validate file extension
            var extension = Path.GetExtension(originalFilename).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                return new RamsPhotoUploadResult
                {
                    Success = false,
                    Message = "Invalid file type. Only JPG and PNG are allowed."
                };
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photoStream.CopyToAsync(fileStream);
            }

            // Save metadata to database
            var ramsPhoto = new RamsPhoto
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                SiteId = siteId,
                CapturedAt = capturedAt,
                UploadedAt = DateTime.UtcNow,
                FilePath = filePath,
                FileSizeBytes = fileSize,
                OriginalFilename = originalFilename
            };

            await _repository.AddAsync(ramsPhoto);

            _logger.LogInformation("✅ RAMS photo uploaded: {PhotoId}, User: {UserId}, Site: {SiteId}, Size: {Size}KB",
                ramsPhoto.Id, userId, siteId, fileSize / 1024);

            return new RamsPhotoUploadResult
            {
                Success = true,
                Message = "Photo uploaded successfully",
                PhotoId = ramsPhoto.Id,
                UploadedAt = ramsPhoto.UploadedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload RAMS photo");
            return new RamsPhotoUploadResult
            {
                Success = false,
                Message = "Failed to upload photo"
            };
        }
    }

    public async Task<List<RamsPhoto>> GetUserPhotosAsync(string userId, int limit = 50)
    {
        return await _repository.GetByUserIdAsync(userId, limit);
    }

    public async Task<List<RamsPhoto>> GetSitePhotosAsync(string siteId)
    {
        return await _repository.GetBySiteIdAsync(siteId);
    }
}

public class RamsPhotoUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PhotoId { get; set; }
    public DateTime? UploadedAt { get; set; }
}