using MetadataExtractor;
using SkiaSharp;
using Pickture.Shared.Models;
using Pickture.Shared.Constants;
using System.IO;
using Directory = System.IO.Directory;

namespace Pickture.Shared.Services;

public interface IImageService
{
    Task<ImageItem?> LoadImageAsync(string filePath);
    Task<List<ImageItem>> ScanFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    byte[]? ExtractExifThumbnail(string filePath);
}

public class ImageService : IImageService
{
    public async Task<ImageItem?> LoadImageAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(filePath) || !Constants.ImageExtensions.IsSupportedImage(filePath))
                return null;

            try
            {
                var fileInfo = new FileInfo(filePath);

                return new ImageItem
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    ModifiedDate = fileInfo.LastWriteTime,
                    FileSizeBytes = fileInfo.Length
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image {filePath}: {ex.Message}");
                return null;
            }
        });
    }

    public async Task<List<ImageItem>> ScanFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var result = new List<ImageItem>();

        if (!Directory.Exists(folderPath))
            return result;

        try
        {
            var files = Directory.GetFiles(folderPath)
                .Where(Constants.ImageExtensions.IsSupportedImage)
                .OrderBy(f => new FileInfo(f).Name)
                .ToList();

            // Load metadata for all files quickly - no thumbnail generation
            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var image = await LoadImageAsync(filePath);
                if (image != null)
                    result.Add(image);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scanning folder {folderPath}: {ex.Message}");
        }

        return result;
    }

    public byte[]? ExtractExifThumbnail(string filePath)
    {
        try
        {
            // Attempt to extract EXIF thumbnail
            // This is a simplified approach - actual EXIF extraction depends on file format
            // For now, we'll attempt with JPEG files
            if (!filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && 
                !filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                return null;

            var directories = ImageMetadataReader.ReadMetadata(filePath);
            var exifDir = directories.FirstOrDefault();

            // Check if we can extract thumbnail data
            // This would require checking the actual EXIF structure
            // For now, return null to fall back to generation
            return null;
        }
        catch
        {
            // Fall through to generation
            return null;
        }
    }
}
