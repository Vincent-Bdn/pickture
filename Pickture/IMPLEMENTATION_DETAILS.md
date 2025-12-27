# Implementation Details

## Application Flow

### Entry Point (App.xaml.cs)
```
App.CreateWindow() 
  → Creates Window with FolderSelectionPage
```

### User Journey

1. **Folder Selection** (`Features/FolderSelection/`)
   - User sees welcome screen with "Open a folder" button
   - Click opens folder selection dialog
   - Dialog returns path or defaults to Pictures folder
   - ViewModel triggers `FolderSelected` event
   - Application navigates to `ImageGalleryPage`

2. **Image Gallery** (`Features/ImageGallery/`)
   - Page constructor receives folder path
   - `OnAppearing` triggers async folder scan
   - `ImageService.ScanFolderAsync()` loads all images
   - Images are added to observable collection as they're processed
   - UI updates in real-time as images load
   - User can start navigating immediately (shows loaded thumbnails)

## Key Components

### ImageService (Shared/Services/ImageService.cs)

**Responsibilities:**
- Load individual images with metadata
- Scan entire folders and build image lists
- Generate thumbnails from large images
- Cache thumbnails to avoid reprocessing
- Extract EXIF data (prepared for future use)

**Key Methods:**
```csharp
LoadImageAsync(filePath)          // Load single image with thumbnail
ScanFolderAsync(folderPath)       // Scan folder and load all images asynchronously
ExtractExifThumbnail(filePath)    // Try to extract embedded EXIF thumbnail
GenerateThumbnail(filePath)       // Generate thumbnail using SkiaSharp
```

**Thumbnail Generation:**
- Uses SkiaSharp for high-quality downscaling
- Default size: 200x150 pixels
- JPEG encoding at 80% quality for balance between size and quality
- Cached in memory to prevent regeneration

### ViewModels

#### ImageGalleryViewModel
```csharp
SelectedImage              // Currently displayed image
SelectedImageIndex         // Index in the images collection
Images                     // ObservableCollection of all images
IsLoading                  // Loading state indicator

NavigateNext()             // Move to next image
NavigatePrevious()         // Move to previous image
ToggleFavorite()           // Mark/unmark current image as favorite
RequestChangeFolder()      // Go back to folder selection
RequestExit()              // Quit application
```

#### FolderSelectionViewModel
```csharp
SelectedFolderPath         // Path of selected folder
FolderSelected             // Event when folder is chosen

SelectFolderAsync()        // Open folder picker dialog
```

### Image Data Model

```csharp
public class ImageItem
{
    string FilePath         // Full file path
    string FileName         // File name only
    byte[] ThumbnailData    // JPEG-encoded thumbnail
    bool IsFavorite         // Star status
    DateTime ModifiedDate   // File modification time
    long FileSizeBytes      // File size
}
```

## UI Components

### FolderSelectionPage
- **Type**: XAML-based ContentPage
- **Content**: Welcome label, load button, description
- **Styling**: Centered layout with blue primary button
- **Code-behind**: Handles button click and folder selection

### ImageGalleryPage
- **Type**: XAML-based ContentPage with 2-column grid layout
- **Left Column**: 200px wide thumbnail panel
- **Right Column**: Main image display area
- **Behavior**: 
  - Loads images asynchronously when page appears
  - Updates main image when thumbnail is selected
  - Supports keyboard navigation

### ThumbnailItemControl
- **Type**: Custom ContentView control
- **Content**: Thumbnail image + filename + favorite indicator
- **Binding**: Takes `ImageItem` as BindableProperty
- **Styling**: Frame with rounded corners and shadow

## Asynchronous Loading

### Folder Scanning Flow
1. User selects folder
2. Navigation to ImageGalleryPage with folder path
3. Page.OnAppearing() triggers async scan
4. `ImageService.ScanFolderAsync()` begins enumeration
5. For each image file:
   - Load image metadata
   - Generate/cache thumbnail
   - Create ImageItem
   - Add to observable collection
   - Small 5ms delay for UI responsiveness
6. UI updates as images are added to collection
7. If cancelled (folder change), CancellationToken stops processing

### Thumbnail Caching
- Dictionary<string, byte[]> keeps processed thumbnails in memory
- Check cache before regenerating
- Survives for lifetime of application
- Could be enhanced with disk caching for very large folders

## Image Format Support

Supported extensions (case-insensitive):
- `.jpg`, `.jpeg` - JPEG
- `.png` - PNG
- `.gif` - GIF
- `.bmp` - Bitmap
- `.webp` - WebP
- `.tiff`, `.tif` - TIFF
- `.heic`, `.heif` - HEIC/HEIF (Apple formats)

Determined by `ImageExtensions.SupportedExtensions` constant.

## Keyboard Input

Currently supported:
- **Up/Left**: Navigate to previous image
- **Down/Right**: Navigate to next image
- **Back Button**: Return to folder selection

Implemented in `ImageGalleryPage.OnAppearing()` with focus request and back button override.

Note: Full keyboard event handling with PreviewKeyDown would require platform-specific implementation. Current implementation is simplified for basic navigation.

## Data Binding

Uses standard MAUI data binding with INotifyPropertyChanged:
- ObservableCollection for auto-updating lists
- Binding on SelectedImage for main display
- Two-way binding where applicable
- Value converters for image byte[] to ImageSource conversion

## Performance Characteristics

### Memory Usage
- Stores thumbnail byte[] for each image in memory
- ~10-50KB per thumbnail (depending on image complexity)
- 1000 images ≈ 10-50 MB thumbnail cache
- Full folder list in ObservableCollection

### Processing Time
- Thumbnail generation: ~50-200ms per image (depends on image size and complexity)
- Loading 100 images: ~5-20 seconds (with UI updates)
- Scrolling: Instant (already loaded thumbnails)

### Optimization Opportunities
1. **Virtualization**: Implement CollectionView virtualization for 10,000+ images
2. **Disk Caching**: Save thumbnails to disk (e.g., hidden .pickture folder)
3. **EXIF Extraction**: Use embedded thumbnails from camera RAW files
4. **Progressive Loading**: Load higher-quality thumbnails for visible items only
5. **Parallel Processing**: Load multiple images simultaneously with Task.WhenAll

## Future EXIF Implementation

MetadataExtractor is included for future EXIF reading:
```csharp
// Would read image metadata
var directories = ImageMetadataReader.ReadMetadata(filePath);

// Access EXIF directory for camera info
var exifDir = directories.OfType<IExifDirectory>().FirstOrDefault();

// Read specific tags
var isoSpeed = exifDir?.GetIsoSpeedRating();
var focalLength = exifDir?.GetFocalLength();
var cameraModel = exifDir?.GetCameraModel();
```

This infrastructure is ready for:
- Display camera settings on image info panel
- Filter by camera settings
- Sort by EXIF metadata
- Extract embedded thumbnails from RAW files

## Platform-Specific Considerations

### Windows (Primary)
- Native folder picker support (future enhancement with Windows.Win32)
- Full keyboard support
- Desktop window chrome

### macOS/Linux (Secondary)
- Defaults to home pictures folder
- Same UI and functionality
- Requires testing for platform-specific issues

### Android/iOS (Possible Future)
- Would require different folder selection approach
- Touch-friendly UI already implemented
- Network folder access considerations
- Camera roll integration possible

## Testing Considerations

### Unit Testing
- ImageService can be tested with mock file systems
- ViewModels can be tested independently
- Converters can be tested with sample data

### Integration Testing
- Create test folder with sample images
- Verify folder scanning completes
- Verify thumbnail generation produces valid images
- Verify navigation state management

### Performance Testing
- Test with folders containing 100, 1000, 10000+ images
- Measure memory usage and UI responsiveness
- Profile thumbnail generation time
- Test cancellation behavior
