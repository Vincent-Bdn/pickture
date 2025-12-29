using Pickture.Shared.Services;

namespace Pickture.Shared.Controls;

/// <summary>
/// Custom image view that loads thumbnails asynchronously on demand
/// </summary>
public class AsyncThumbnailImage : Image
{
    private static IThumbnailCacheService? _thumbnailCache;
    private string? _currentFilePath;

    public static readonly BindableProperty FilePathProperty =
        BindableProperty.Create(
            nameof(FilePath),
            typeof(string),
            typeof(AsyncThumbnailImage),
            null,
            propertyChanged: OnFilePathChanged);

    public string? FilePath
    {
        get => (string?)GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    private static void OnFilePathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AsyncThumbnailImage control && newValue is string filePath)
        {
            control.LoadThumbnailAsync(filePath);
        }
    }

    private async void LoadThumbnailAsync(string filePath)
    {
        if (_currentFilePath == filePath)
            return;

        _currentFilePath = filePath;
        _thumbnailCache ??= new ThumbnailCacheService();

        try
        {
            var thumbnail = await _thumbnailCache.GetThumbnailAsync(filePath);
            
            if (thumbnail != null && _currentFilePath == filePath) // Still the same file
            {
                Source = ImageSource.FromStream(() => new MemoryStream(thumbnail));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading thumbnail for {filePath}: {ex.Message}");
        }
    }
}
