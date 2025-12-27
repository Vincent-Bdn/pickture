using SkiaSharp;

namespace Pickture.Shared.Services;

/// <summary>
/// Manages thumbnail generation and caching separately from the model.
/// Thumbnails are generated on-demand and cached to avoid redundant processing.
/// </summary>
public interface IThumbnailCacheService
{
    Task<byte[]?> GetThumbnailAsync(string filePath, int maxWidth = 200, int maxHeight = 150);
    void ClearCache();
}

public class ThumbnailCacheService : IThumbnailCacheService
{
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(4); // 4 concurrent thumbnail generations

    public async Task<byte[]?> GetThumbnailAsync(string filePath, int maxWidth = 200, int maxHeight = 150)
    {
        // Return cached thumbnail if available
        if (_cache.TryGetValue(filePath, out var cached))
            return cached;

        // Generate thumbnail with concurrency control
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring semaphore
            if (_cache.TryGetValue(filePath, out var cached2))
                return cached2;

            var thumbnail = await GenerateThumbnailAsync(filePath, maxWidth, maxHeight);
            
            if (thumbnail != null)
            {
                _cache[filePath] = thumbnail;
            }

            return thumbnail;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    private static async Task<byte[]?> GenerateThumbnailAsync(string filePath, int maxWidth, int maxHeight)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                using var bitmap = SKBitmap.Decode(filePath);
                if (bitmap == null)
                    return null;

                var scale = Math.Min(
                    (float)maxWidth / bitmap.Width,
                    (float)maxHeight / bitmap.Height
                );

                var newWidth = (int)(bitmap.Width * scale);
                var newHeight = (int)(bitmap.Height * scale);

                using var resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
                using var image = SKImage.FromBitmap(resized);
                using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 80);

                return encoded.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating thumbnail for {filePath}: {ex.Message}");
                return null;
            }
        });
    }
}
