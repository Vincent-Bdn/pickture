# Pickture - Image Gallery Application

A lightweight, high-performance image viewer for photographers using MAUI (.NET Multi-platform App UI) with a focus on handling large image collections efficiently.

## Features

- **Empty start page** with folder selection dialog
- **Three-part responsive UI** once a folder is selected:
  - **Top Navigation Bar**: File menu with "Change Folder" and "Exit" options
  - **Left Thumbnail Panel**: Shows all images in the selected folder with star favorites, responsive to keyboard navigation (Up/Down)
  - **Main Display Area**: Shows the selected image in full quality
- **EXIF Thumbnail Extraction**: Automatically extracts thumbnails from EXIF metadata to avoid processing overhead
- **Thumbnail Generation**: For images without EXIF thumbnails, generates them using SkiaSharp
- **Buffered/Virtualized Loading**: Loads images asynchronously and displays them progressively to handle thousands of images efficiently
- **Keyboard Navigation**: Use Up/Down arrow keys to navigate through images
- **Favorite Marking**: Star images as favorites (infrastructure ready for future persistence)

## Architecture

### Vertical Slice Architecture

The application follows Vertical Slice Architecture principles where each feature is self-contained within its own folder with no cross-feature communication:

```
Pickture/
├── Features/                          # Feature folders - each is independent
│   ├── FolderSelection/              # Feature: Initial folder selection
│   │   ├── FolderSelectionPage.xaml
│   │   ├── FolderSelectionPage.xaml.cs
│   │   └── FolderSelectionViewModel.cs
│   └── ImageGallery/                 # Feature: Image gallery and viewing
│       ├── ImageGalleryPage.xaml
│       ├── ImageGalleryPage.xaml.cs
│       ├── ImageGalleryViewModel.cs
│       ├── ThumbnailItemControl.xaml
│       └── ThumbnailItemControl.xaml.cs
├── Shared/                            # Shared utilities and services
│   ├── Services/
│   │   └── ImageService.cs           # Image loading and EXIF handling
│   ├── Models/
│   │   └── ImageItem.cs              # Image data model
│   ├── Constants/
│   │   └── ImageExtensions.cs        # Supported image formats
│   ├── Converters/
│   │   └── ImageConverters.cs        # XAML value converters
│   └── Behaviors/
│       └── KeyboardBehavior.cs       # Keyboard handling
└── App.xaml/xaml.cs                  # Application entry point
```

### Key Components

#### ImageService (Shared/Services)
- **EXIF Thumbnail Extraction**: Uses MetadataExtractor library to read EXIF data
- **Thumbnail Generation**: Uses SkiaSharp for high-quality thumbnail generation
- **Caching**: In-memory thumbnail cache to avoid reprocessing
- **Folder Scanning**: Asynchronous scanning with cancellation support

#### ImageGalleryViewModel
- Manages state for the gallery view
- Handles image navigation (next/previous)
- Manages favorite toggling
- Coordinates with ImageService for data loading
- Implements MVVM pattern with INotifyPropertyChanged

#### FolderSelectionViewModel
- Handles folder selection via native Windows file dialog
- Triggers navigation to gallery view

## Dependencies

- **MAUI**: Cross-platform UI framework
- **SkiaSharp 2.88.8**: Image rendering and thumbnail generation
- **MetadataExtractor 2.8.1**: EXIF metadata extraction
- **System.Windows.Forms**: For native folder selection dialog on Windows

## Building and Running

### Prerequisites
- .NET 9 SDK
- MAUI workload installed (`dotnet workload install maui`)

### Commands

```bash
# Restore dependencies
dotnet restore

# Build for Windows
dotnet build -f net9.0-windows10.0.19041.0

# Run the application
dotnet run -f net9.0-windows10.0.19041.0
```

## Platform Support

- **Primary**: Windows 10/11
- **Supported**: iOS, Android, macOS (with MAUI's cross-platform capabilities)

Note: Folder selection uses Windows Forms on Windows platform; other platforms fall back to system picture folders.

## Usage

1. **Launch Application**: Opens with an empty page and "Open a folder" button
2. **Select Folder**: Click the button to choose a folder containing images
3. **View Gallery**: 
   - Left panel shows all image thumbnails
   - Click a thumbnail to view it in the main area
   - Use Up/Down arrow keys to navigate
   - Click "Favorite" button to mark images as favorites
4. **Change Folder**: Use File menu → "Change Folder"
5. **Exit**: Use File menu → "Exit Pickture" or close the window

## Performance Considerations

- **Lazy Loading**: Images are loaded asynchronously to maintain UI responsiveness
- **EXIF Optimization**: Extracts thumbnails from EXIF first to avoid full image processing
- **In-Memory Caching**: Thumbnail cache prevents reprocessing
- **Buffering**: Infrastructure for virtualized list loading (ready for future optimization with large folders)

## Future Enhancements

- [ ] Persistent favorite storage (SQLite/JSON)
- [ ] Image metadata display (EXIF data viewer)
- [ ] Slideshow mode
- [ ] Rating system
- [ ] Image collection management
- [ ] Full virtualization for 10,000+ image folders
- [ ] Advanced filtering and search
- [ ] Export functionality

## Architecture Benefits

**Vertical Slice Approach**:
- Features are isolated and can be developed/tested independently
- Easy to add new features without affecting existing code
- Clear separation of concerns
- Scalable: Adding new image processing features doesn't require modifying core code

**Shared Layer**:
- Common models, services, and utilities used by features
- Image handling centralized in ImageService
- Extensible for new features without duplication
