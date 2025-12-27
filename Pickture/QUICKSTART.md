# Pickture - Quick Start Guide

## Project Setup

This is a C# MAUI desktop application for high-performance image viewing, optimized for photographers working with large image collections.

### Prerequisites

- .NET 9 SDK (or newer)
- MAUI workload installed

### Installation

1. Install MAUI workload (if not already installed):
```bash
dotnet workload install maui
```

2. Navigate to the project directory:
```bash
cd C:\Perso\pickture\Pickture
```

3. Restore NuGet packages:
```bash
dotnet restore
```

### Building

Build for Windows:
```bash
dotnet build -f net9.0-windows10.0.19041.0
```

### Running

Run the application:
```bash
dotnet run -f net9.0-windows10.0.19041.0
```

## Application Features

### Initial Screen
- Shows a welcome screen with "Open a folder" button
- Click the button to select a folder containing images

### Gallery View
Once a folder is selected, you'll see:

1. **Top Navigation Bar**
   - File menu with options:
     - "Change Folder" - select a different folder
     - "Exit Pickture" - close the application

2. **Left Panel**
   - Lists all images in the selected folder as scrollable thumbnails
   - Shows image filename and favorite star indicator
   - Click any thumbnail to view it in the main area
   - Responsive to keyboard navigation (Up/Down arrows)

3. **Main Display Area**
   - Shows the selected image in full size
   - Maintains aspect ratio with padding
   - Navigation buttons: Previous/Next
   - Favorite button to mark/unmark images
   - Displays current image filename

### Keyboard Navigation
- **Up Arrow / Left Arrow**: Navigate to previous image
- **Down Arrow / Right Arrow**: Navigate to next image
- **Back Button**: Return to folder selection screen

## Project Structure

### Vertical Slice Architecture
The application uses Vertical Slice Architecture where each feature is completely independent:

#### Features/FolderSelection/
- Initial folder selection interface
- Cross-platform folder picker (defaults to Pictures folder on non-Windows platforms)

#### Features/ImageGallery/
- Main image viewing and gallery interface
- Thumbnail display
- Image navigation and interaction
- Favorite management

#### Shared/
- **Services/ImageService.cs**: Handles image loading, thumbnail generation using SkiaSharp
- **Models/ImageItem.cs**: Data model for image information
- **Constants/ImageExtensions.cs**: Supported image file formats
- **Converters/ImageConverters.cs**: XAML value converters for image binding
- **Behaviors/**: Custom MAUI behaviors for interactions

## Technology Stack

- **MAUI 9.0**: Cross-platform UI framework
- **SkiaSharp 2.88.8**: High-performance image rendering and thumbnail generation
- **MetadataExtractor 2.8.1**: EXIF metadata reading (future enhancement)

## Performance Optimizations

1. **Asynchronous Loading**: Images load in the background without blocking the UI
2. **Thumbnail Caching**: Generated thumbnails are cached to prevent reprocessing
3. **SkiaSharp Rendering**: Efficient image scaling and rendering using SkiaSharp instead of MAUI's native image controls
4. **Cancellation Support**: Folder scanning can be cancelled when changing folders

## Future Enhancements

- [ ] EXIF thumbnail extraction from camera images
- [ ] Persistent favorite storage (JSON/SQLite)
- [ ] Image metadata viewer (camera settings, ISO, f-stop, etc.)
- [ ] Slideshow mode
- [ ] Full virtualization for 10,000+ image folders
- [ ] Image filtering and search
- [ ] RAW file support
- [ ] Batch operations
- [ ] Custom keyboard shortcuts
- [ ] Dark/Light theme toggle

## Troubleshooting

### Application won't start
- Ensure .NET 9 SDK is installed: `dotnet --version`
- Ensure MAUI workload is installed: `dotnet workload list`
- Clean and rebuild: `dotnet clean && dotnet build`

### Images not loading
- Ensure the folder path is correct
- Check that image files have supported extensions (.jpg, .jpeg, .png, .gif, .bmp, .webp, .tiff, .heic, etc.)
- Check application output for detailed error messages

### Slow thumbnail generation
- Large folder with thousands of images will take time on first load
- Thumbnails are cached after generation for faster subsequent access
- For very large folders (10,000+ images), consider using the planned virtualization feature

## Architecture Notes

### Why Vertical Slice Architecture?
- **Scalability**: New features can be added without modifying existing code
- **Testability**: Each feature can be tested independently
- **Maintainability**: Clear separation of concerns makes the codebase easier to understand
- **Flexibility**: Features can be developed in parallel by different team members

### Why SkiaSharp?
- **Performance**: Efficient image rendering without GDI+ overhead
- **Cross-platform**: Works on Windows, Linux, macOS, Android, iOS
- **High quality**: Professional-grade image scaling and rendering
- **Small footprint**: Minimal memory usage compared to full image libraries

## Development Notes

The application is designed to be platform-agnostic while prioritizing Windows desktop performance. MAUI allows running the same codebase on multiple platforms with minimal modifications.

Cross-platform considerations:
- Folder selection defaults to Pictures folder on non-Windows platforms
- File paths are handled using System.IO which abstracts platform differences
- All image rendering uses SkiaSharp for consistency across platforms
