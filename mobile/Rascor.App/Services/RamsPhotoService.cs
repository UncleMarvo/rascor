using Microsoft.Extensions.Logging;
using Rascor.App.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Rascor.App.Services;

public class RamsPhotoService
{
    private readonly ILogger<RamsPhotoService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _photosDirectory;
    private readonly BackendApi? _backendApi;

    public RamsPhotoService(
        ILogger<RamsPhotoService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Store photos in app's local data directory
        _photosDirectory = Path.Combine(FileSystem.AppDataDirectory, "RamsPhotos");

        // Create directory if it doesn't exist
        if (!Directory.Exists(_photosDirectory))
        {
            Directory.CreateDirectory(_photosDirectory);
            _logger.LogInformation("Created RAMS photos directory: {Path}", _photosDirectory);
        }
    }

    /// <summary>
    /// Take a photo using the device camera
    /// </summary>
    public async Task<RamsPhoto?> TakePhotoAsync(string userId, string siteId, string siteName)
    {
        try
        {
            _logger.LogInformation("📸 Starting camera capture...");

            // Check if camera is available
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                _logger.LogWarning("Camera not supported on this device");
                return null;
            }

            // Take photo
            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"RAMS Form - {siteName}"
            });

            if (photo == null)
            {
                _logger.LogInformation("User cancelled photo capture");
                return null;
            }

            _logger.LogInformation("Photo captured, opening stream...");

            // Generate unique filename
            var fileName = $"rams_{siteId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var targetPath = Path.Combine(_photosDirectory, fileName);

            _logger.LogInformation("Target path: {Path}", targetPath);

            // Copy and compress the photo
            try
            {
                using (var sourceStream = await photo.OpenReadAsync())
                {
                    _logger.LogInformation("Source stream opened, size: {Size} bytes", sourceStream.Length);

                    using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }
                }

                _logger.LogInformation("Photo copied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy photo file");
                return null;
            }

            // Verify file was created
            if (!File.Exists(targetPath))
            {
                _logger.LogError("Photo file was not created at {Path}", targetPath);
                return null;
            }

            // Get file size
            var fileInfo = new FileInfo(targetPath);
            _logger.LogInformation("Photo saved: {Size}KB", fileInfo.Length / 1024);

            // Compress if too large (> 2MB)
            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                _logger.LogInformation("Photo is {Size}MB, compressing...", fileInfo.Length / 1024.0 / 1024.0);
                await CompressImageAsync(targetPath);
                fileInfo = new FileInfo(targetPath); // Refresh file info
                _logger.LogInformation("After compression: {Size}KB", fileInfo.Length / 1024);
            }

            var ramsPhoto = new RamsPhoto
            {
                UserId = userId,
                SiteId = siteId,
                SiteName = siteName,
                CapturedAt = DateTime.Now,
                LocalFilePath = targetPath,
                FileSizeBytes = fileInfo.Length,
                IsUploaded = false
            };

            _logger.LogInformation("RAMS photo captured: {FileName}, Size: {Size}KB",
                fileName, fileInfo.Length / 1024);

            return ramsPhoto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture RAMS photo");
            return null;
        }
    }

    /// <summary>
    /// Pick existing photo from gallery
    /// </summary>
    public async Task<RamsPhoto?> PickPhotoAsync(string userId, string siteId, string siteName)
    {
        try
        {
            _logger.LogInformation("📸 Opening gallery picker...");

            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select RAMS Form Photo"
            });

            if (photo == null)
            {
                _logger.LogInformation("User cancelled photo selection");
                return null;
            }

            _logger.LogInformation("Photo selected, opening stream...");

            // Generate unique filename
            var fileName = $"rams_{siteId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var targetPath = Path.Combine(_photosDirectory, fileName);

            _logger.LogInformation("Target path: {Path}", targetPath);

            // Copy photo to app directory
            try
            {
                using (var sourceStream = await photo.OpenReadAsync())
                {
                    _logger.LogInformation("Source stream opened, size: {Size} bytes", sourceStream.Length);

                    using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }
                }

                _logger.LogInformation("Photo copied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy photo file");
                return null;
            }

            // Verify file was created
            if (!File.Exists(targetPath))
            {
                _logger.LogError("Photo file was not created at {Path}", targetPath);
                return null;
            }

            // Get file size
            var fileInfo = new FileInfo(targetPath);
            _logger.LogInformation("Photo saved: {Size}KB", fileInfo.Length / 1024);

            // Compress if too large
            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                _logger.LogInformation("Photo is {Size}MB, compressing...", fileInfo.Length / 1024.0 / 1024.0);
                await CompressImageAsync(targetPath);
                fileInfo = new FileInfo(targetPath);
                _logger.LogInformation("After compression: {Size}KB", fileInfo.Length / 1024);
            }

            var ramsPhoto = new RamsPhoto
            {
                UserId = userId,
                SiteId = siteId,
                SiteName = siteName,
                CapturedAt = DateTime.Now,
                LocalFilePath = targetPath,
                FileSizeBytes = fileInfo.Length,
                IsUploaded = false
            };

            _logger.LogInformation("✅ RAMS photo selected successfully: {FileName}", fileName);

            return ramsPhoto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pick RAMS photo");
            return null;
        }
    }

    /// <summary>
    /// Compress image to reduce file size
    /// </summary>
    private async Task CompressImageAsync(string filePath)
    {
        try
        {
            // Load the image
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath);

            // Calculate new dimensions (max 1920px width, maintain aspect ratio)
            int maxWidth = 1920;
            int maxHeight = 1920;

            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);

            // Resize
            image.Mutate(x => x.Resize(newWidth, newHeight));

            // Save with compression
            var tempPath = filePath + ".tmp";
            await image.SaveAsJpegAsync(tempPath, new JpegEncoder
            {
                Quality = 85 // Good balance between quality and file size
            });

            // Replace original with compressed version
            File.Delete(filePath);
            File.Move(tempPath, filePath);

            _logger.LogInformation("Image compressed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress image");
            // Don't throw - just keep original
        }
    }

    /// <summary>
    /// Get today's RAMS photos
    /// </summary>
    public List<RamsPhoto> GetTodaysPhotos()
    {
        // For now, scan directory for today's files
        // Later, we'll use a proper database
        var today = DateTime.Today;
        var photos = new List<RamsPhoto>();

        try
        {
            var files = Directory.GetFiles(_photosDirectory, "rams_*.jpg");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime.Date == today)
                {
                    // Parse filename: rams_{siteId}_{timestamp}.jpg
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var parts = fileName.Split('_');

                    if (parts.Length >= 3)
                    {
                        photos.Add(new RamsPhoto
                        {
                            SiteId = parts[1],
                            LocalFilePath = file,
                            CapturedAt = fileInfo.CreationTime,
                            FileSizeBytes = fileInfo.Length,
                            IsUploaded = false // TODO: Check if uploaded
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get today's photos");
        }

        return photos.OrderByDescending(p => p.CapturedAt).ToList();
    }

    /// <summary>
    /// Delete a photo
    /// </summary>
    public bool DeletePhoto(RamsPhoto photo)
    {
        try
        {
            if (File.Exists(photo.LocalFilePath))
            {
                File.Delete(photo.LocalFilePath);
                _logger.LogInformation("Deleted RAMS photo: {Path}", photo.LocalFilePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete photo");
            return false;
        }
    }

    /// <summary>
    /// Upload photo to backend
    /// </summary>
    public async Task<bool> UploadPhotoAsync(RamsPhoto photo)
    {

        // TODO: Enable cloud upload in v1.1
        _logger.LogInformation("📸 Photo saved locally at: {Path}", photo.LocalFilePath);
        photo.IsUploaded = false; // Mark as not uploaded
        return true; // Return success so UI shows "Photo saved"

        //try
        //{
        //    _logger.LogWarning("🔵🔵 Resolving BackendApi...");
        //    var backendApi = _serviceProvider.GetRequiredService<BackendApi>();

        //    if (backendApi == null)
        //    {
        //        _logger.LogError("🔵🔵🔵 Failed to resolve BackendApi from service provider");
        //        return false;
        //    }
            
        //    var success = await backendApi.UploadRamsPhotoAsync(photo);
        //    if (success)
        //    {
        //        photo.IsUploaded = true;
        //        photo.UploadedAt = DateTime.Now;
        //        _logger.LogInformation("RAMS photo uploaded: {PhotoId}", photo.Id);
        //    }

        //    return success;
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "🔴 Exception in UploadPhotoAsync: {Message}", ex.Message);
        //    return false;
        //}
    }
}
