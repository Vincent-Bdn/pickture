# Pickture - Developer Guide

## Getting Started

### System Requirements
- **OS**: Windows 10/11 (primary development platform)
- **.NET**: 9.0 SDK or later
- **RAM**: 4GB minimum (8GB recommended)
- **Disk**: 2GB for SDK and dependencies

### Initial Setup

1. **Verify .NET Installation**
   ```powershell
   dotnet --version
   ```
   Should show 9.0 or later.

2. **Install MAUI Workload**
   ```powershell
   dotnet workload install maui
   ```

3. **Clone/Navigate to Project**
   ```powershell
   cd C:\Perso\pickture\Pickture
   ```

4. **Restore Dependencies**
   ```powershell
   dotnet restore
   ```

5. **Build Project**
   ```powershell
   dotnet build -f net9.0-windows10.0.19041.0
   ```

6. **Run Application**
   ```powershell
   dotnet run -f net9.0-windows10.0.19041.0
   ```

## Project Organization

### Vertical Slice Architecture Explanation

Rather than organizing by technical layer (Models, Views, Services), we organize by feature/business capability:

```
Features/                    # Each feature is complete and independent
├── FolderSelection/        # Feature: User selects a folder
│   ├── Page               # UI (XAML)
│   ├── ViewModel          # Logic & State
│   └── (Services)         # Feature-specific services (if any)
└── ImageGallery/          # Feature: View and navigate images
    ├── Page
    ├── ViewModel
    └── Controls           # Reusable UI components

Shared/                     # Shared across ALL features
├── Services/             # Cross-feature services (ImageService)
├── Models/               # Data models (ImageItem)
├── Constants/            # Constants (ImageExtensions)
├── Converters/           # XAML value converters
└── Behaviors/            # Custom behaviors
```

### Why This Structure?

**Advantages:**
- ✅ Easy to find code for a specific feature
- ✅ Features can be developed independently
- ✅ Minimal cross-feature dependencies
- ✅ Easy to remove or refactor a feature
- ✅ Scales well as project grows

**vs. Layer-Based (Models/Views/Services):**
- ❌ Scattered feature code across multiple folders
- ❌ Hard to understand what a feature needs
- ❌ Difficult to remove features without breaking others
- ❌ Doesn't scale well

## Code Patterns Used

### 1. MVVM (Model-View-ViewModel)

**ViewModel** (e.g., `ImageGalleryViewModel.cs`):
```csharp
public class ImageGalleryViewModel : INotifyPropertyChanged
{
    // Properties with change notification
    private ObservableCollection<ImageItem> _images = new();
    public ObservableCollection<ImageItem> Images 
    { 
        get => _images;
        set { if (_images != value) { _images = value; OnPropertyChanged(); } }
    }
    
    // Commands and methods
    public void NavigateNext() => SelectedImageIndex++;
    
    // Events
    public event Action<string>? FolderChangeRequested;
}
```

**Page/View** (e.g., `ImageGalleryPage.xaml.cs`):
```csharp
public partial class ImageGalleryPage : ContentPage
{
    private readonly ImageGalleryViewModel _viewModel;
    
    public ImageGalleryPage(string folderPath)
    {
        InitializeComponent();
        _viewModel = new ImageGalleryViewModel(_imageService);
        BindingContext = _viewModel;  // Connect ViewModel to View
    }
}
```

**XAML Binding** (e.g., `ImageGalleryPage.xaml`):
```xaml
<Image Source="{Binding SelectedImage.FilePath, Converter={StaticResource FullImageConverter}}"/>
```

### 2. Dependency Injection Pattern

Services are instantiated and passed to ViewModels:
```csharp
var imageService = new ImageService();
var viewModel = new ImageGalleryViewModel(imageService);
```

Can be extended to use MAUI's DI container:
```csharp
// In MauiProgram.cs
services.AddSingleton<IImageService, ImageService>();
services.AddSingleton<ImageGalleryViewModel>();
```

### 3. Async/Await for Long-Running Operations

```csharp
// ImageService method signature
public async Task<ImageItem?> LoadImageAsync(string filePath)
{
    // Database call, file I/O, etc.
    return await Task.Run(() => { /* heavy work */ });
}

// Called from ViewModel
public async Task LoadFolderAsync(string folderPath)
{
    var images = await _imageService.ScanFolderAsync(folderPath);
}

// Called from View
protected override async void OnAppearing()
{
    await _viewModel.LoadFolderAsync(_folderPath);
}
```

### 4. Observable Collections for UI Binding

```csharp
// Automatically updates UI when collection changes
public ObservableCollection<ImageItem> Images { get; set; }

// Add/Remove/Update operations automatically refresh UI
Images.Add(newImage);       // UI updates immediately
Images[0] = updatedImage;   // UI updates immediately
```

### 5. Value Converters for XAML Binding

```csharp
public class ThumbnailConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] data && data.Length > 0)
            return ImageSource.FromStream(() => new MemoryStream(data));
        return null!;
    }
}

// Used in XAML:
// Source="{Binding ThumbnailData, Converter={StaticResource ThumbnailConverter}}"
```

## Common Development Tasks

### Adding a New Page

1. **Create feature folder**
   ```
   Features/MyFeature/
   ```

2. **Create XAML page**
   ```
   MyFeaturePage.xaml
   ```
   ```xaml
   <?xml version="1.0" encoding="utf-8" ?>
   <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                x:Class="Pickture.Features.MyFeature.MyFeaturePage"
                Title="My Feature">
       <!-- Your UI here -->
   </ContentPage>
   ```

3. **Create code-behind**
   ```
   MyFeaturePage.xaml.cs
   ```
   ```csharp
   namespace Pickture.Features.MyFeature;
   
   public partial class MyFeaturePage : ContentPage
   {
       public MyFeaturePage()
       {
           InitializeComponent();
       }
   }
   ```

4. **Create ViewModel** (optional but recommended)
   ```
   MyFeatureViewModel.cs
   ```

5. **Connect ViewModel to View**
   ```csharp
   var viewModel = new MyFeatureViewModel();
   BindingContext = viewModel;
   ```

### Adding a New Service

1. **Create interface** in `Shared/Services/`
   ```csharp
   public interface IMyService
   {
       Task<string> GetDataAsync(string id);
   }
   ```

2. **Implement interface**
   ```csharp
   public class MyService : IMyService
   {
       public async Task<string> GetDataAsync(string id)
       {
           // Implementation
       }
   }
   ```

3. **Inject into ViewModel**
   ```csharp
   public class MyFeatureViewModel
   {
       private readonly IMyService _service;
       
       public MyFeatureViewModel(IMyService service)
       {
           _service = service;
       }
   }
   ```

### Debugging

**Visual Studio or VS Code:**
- Set breakpoints in code
- Use F5 to debug
- Inspect variables in the watch window
- Step through async code

**Console Output:**
```csharp
System.Diagnostics.Debug.WriteLine($"Value: {someVariable}");
```

**XAML Binding Issues:**
- Set `x:DataType` on pages for binding validation
- Check Output window for binding errors
- Use compiled bindings for better error detection

## Testing Strategy

### Unit Tests (Recommended)

Create a `Pickture.Tests` project:

```csharp
[TestFixture]
public class ImageServiceTests
{
    private ImageService _service;
    
    [SetUp]
    public void Setup()
    {
        _service = new ImageService();
    }
    
    [Test]
    public async Task LoadImageAsync_WithValidPath_ReturnsImageItem()
    {
        var result = await _service.LoadImageAsync("path/to/test/image.jpg");
        Assert.IsNotNull(result);
        Assert.AreEqual("image.jpg", result.FileName);
    }
}
```

### Integration Tests

Test features end-to-end:
- Create test folder with sample images
- Load folder through UI
- Verify images display correctly
- Test navigation and interactions

## Performance Considerations

### Memory Usage
```csharp
// Monitor memory as you add images
// Each thumbnail ~20-50KB
// 1000 images = 20-50 MB

// For optimization, implement disk caching:
// Save thumbnails to: %AppData%/Pickture/cache/
```

### Image Processing
```csharp
// Current: Synchronous thumbnail generation on UI thread
// Better: Use Task.Run to offload to thread pool
// Best: Parallel processing with Parallel.ForEach
```

### UI Updates
```csharp
// Current: Updates as images load (good responsiveness)
// Could batch: Update in groups of 50 images
// Could virtualize: Only render visible thumbnails
```

## Common Issues & Solutions

### Issue: "Unable to find package SkiaSharp"
**Solution**: Check NuGet package version in .csproj matches available versions
```powershell
dotnet nuget search SkiaSharp --exact
```

### Issue: XAML compilation errors
**Solution**: 
1. Clean project: `dotnet clean`
2. Rebuild: `dotnet build`
3. Check Output window for specific errors

### Issue: Images not loading
**Solution**:
1. Verify folder path is correct
2. Check file permissions
3. Ensure image format is supported
4. Check console output for exceptions

### Issue: Slow folder scanning
**Solution**:
1. Currently scans folder sequentially (by design)
2. For optimization, parallelize:
   ```csharp
   await Parallel.ForEachAsync(files, async (file, ct) => {
       var image = await LoadImageAsync(file);
   });
   ```

## Useful Commands

```powershell
# Build for Windows
dotnet build -f net9.0-windows10.0.19041.0

# Run in debug mode
dotnet run -f net9.0-windows10.0.19041.0

# Clean build artifacts
dotnet clean

# Check for outdated packages
dotnet outdated

# Update packages
dotnet add package SkiaSharp --version 2.88.8

# Project analysis
dotnet build -verbosity normal
```

## References

- [MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/mvvm)
- [Data Binding](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/)
- [SkiaSharp Documentation](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)

## Contributing Guidelines

### Code Style
- Use `using` declarations at top of file
- Use `var` for obvious types, explicit types for clarity
- Name private fields with `_` prefix
- Use `=> expression` for simple property getters

### Naming Conventions
- Classes: PascalCase (ImageGalleryPage)
- Methods: PascalCase (LoadImageAsync)
- Properties: PascalCase (SelectedImage)
- Variables: camelCase (selectedImage, _viewModel)
- Constants: UPPER_SNAKE_CASE or PascalCase (ImageExtensions)

### Async Best Practices
- All long-running operations should be async
- Use `await` instead of `.Result` (deadlock prevention)
- Don't use `async void` except for event handlers
- Use `CancellationToken` for cancellable operations

### Comments & Documentation
- Add XML comments to public methods
- Explain "why" not "what" in comments
- Keep comments up-to-date with code changes

## Release Checklist

- [ ] All features working as documented
- [ ] No unhandled exceptions in logs
- [ ] Tested with 100, 1000, 10000+ images
- [ ] Documentation updated
- [ ] Version number bumped
- [ ] Build successfully without warnings
- [ ] Tested on target platform (Windows 10/11)
