namespace Pickture.Shared.Constants;

public static class ImageExtensions
{
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".heic", ".heif"
    };

    public static bool IsSupportedImage(string filePath) =>
        SupportedExtensions.Contains(Path.GetExtension(filePath));
}
