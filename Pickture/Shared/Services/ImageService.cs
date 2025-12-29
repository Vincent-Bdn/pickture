using MetadataExtractor;
using Pickture.Shared.Models;
using Directory = System.IO.Directory;

namespace Pickture.Shared.Services;

public interface IImageService
{
    Task<ImageItem?> LoadImageAsync(string filePath);
    Task<List<ImageItem>> ScanFolderAsync(string folderPath, CancellationToken cancellationToken = default);
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
}
