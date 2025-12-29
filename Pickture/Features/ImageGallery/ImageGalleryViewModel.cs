using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pickture.Shared.Models;
using Pickture.Shared.Services;

namespace Pickture.Features.ImageGallery;

public enum ProcessingMode
{
    Original,
    WhiteBalanceValue,
    WhiteBalanceRGB
}

public enum VisualEffect
{
    None,
    WhiteBalanceValue,
    WhiteBalanceRGB,
    Custom
}

public class ImageGalleryViewModel : INotifyPropertyChanged
{
    private readonly IImageService _imageService;
    private readonly IImageProcessingService _processingService;
    private readonly ProcessedImageCache _imageCache;
    private string _currentFolderPath = string.Empty;
    private ImageItem? _selectedImage;
    private bool _isLoading;
    private bool _isImageLoading;
    private bool _isComputingVisualEffects;
    private bool _isLoadingSelectedVisualEffect;
    private bool _isProcessingCustomEffect;
    private bool _isLoadingHistogram;
    private double _lowClampValue = 0;
    private double _highClampValue = 255;
    private double _gammaValue = 1.0;
    private ObservableCollection<ImageItem> _images = new();
    private List<ImageItem> _allImages = new();
    private CancellationTokenSource? _loadingCts;
    private ImageSource? _previewPerChannel;
    private ImageSource? _previewWhiteBalance;
    private ProcessingMode _processingMode = ProcessingMode.Original;
    private ImageSource? _displayedImage;
    private ImageSource? _originalImageForConfirmation;
    private ImageSource? _visualEffectWBValueImage;
    private ImageSource? _visualEffectWBRGBImage;
    private ImageSource? _visualEffectCustomImage;
    private VisualEffect _selectedVisualEffect = VisualEffect.None;
    private byte[]? _temporaryWhiteBalanceValueData;
    private byte[]? _temporaryWhiteBalanceRGBData;
    private byte[]? _temporaryCustomData;
    private Task? _currentProcessingTask;
    private double _rotationAngle = 0;
    private double _rotation90Degrees = 0;

    public string CurrentFolderPath
    {
        get => _currentFolderPath;
        set
        {
            if (_currentFolderPath != value)
            {
                _currentFolderPath = value;
                OnPropertyChanged();
            }
        }
    }

    public ImageItem? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (_selectedImage == value)
                return;

            // Deselect previous
            if (_selectedImage != null)
                _selectedImage.IsSelected = false;

            _selectedImage = value;

            // Select new
            if (_selectedImage != null)
                _selectedImage.IsSelected = true;

            OnPropertyChanged();

            // Optional: reset processing / rotation
            ProcessingMode = ProcessingMode.Original;
            RotationAngle = 0;
            Rotation90Degrees = 0;

            if (_selectedImage != null && !string.IsNullOrEmpty(_selectedImage.FilePath))
                UpdateDisplayedImage();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsImageLoading
    {
        get => _isImageLoading;
        set
        {
            if (_isImageLoading != value)
            {
                _isImageLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsComputingVisualEffects
    {
        get => _isComputingVisualEffects;
        set
        {
            if (_isComputingVisualEffects != value)
            {
                _isComputingVisualEffects = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoadingSelectedVisualEffect
    {
        get => _isLoadingSelectedVisualEffect;
        set
        {
            if (_isLoadingSelectedVisualEffect != value)
            {
                _isLoadingSelectedVisualEffect = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsProcessingCustomEffect
    {
        get => _isProcessingCustomEffect;
        set
        {
            if (_isProcessingCustomEffect != value)
            {
                _isProcessingCustomEffect = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoadingHistogram
    {
        get => _isLoadingHistogram;
        set
        {
            if (_isLoadingHistogram != value)
            {
                _isLoadingHistogram = value;
                OnPropertyChanged();
            }
        }
    }

    public double LowClampValue
    {
        get => _lowClampValue;
        set
        {
            if (_lowClampValue != value)
            {
                _lowClampValue = value;
                OnPropertyChanged();
            }
        }
    }

    public double HighClampValue
    {
        get => _highClampValue;
        set
        {
            if (_highClampValue != value)
            {
                _highClampValue = value;
                OnPropertyChanged();
            }
        }
    }

    public double GammaValue
    {
        get => _gammaValue;
        set
        {
            if (_gammaValue != value)
            {
                _gammaValue = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ImageItem> Images
    {
        get => _images;
        set
        {
            if (_images != value)
            {
                _images = value;
                OnPropertyChanged();
            }
        }
    }

    public event Action<string>? FolderChangeRequested;
    public event Action? ExitRequested;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ImageSource? PreviewPerChannel
    {
        get => _previewPerChannel;
        set
        {
            if (_previewPerChannel != value)
            {
                _previewPerChannel = value;
                OnPropertyChanged();
            }
        }
    }

    public ImageSource? PreviewWhiteBalance
    {
        get => _previewWhiteBalance;
        set
        {
            if (_previewWhiteBalance != value)
            {
                _previewWhiteBalance = value;
                OnPropertyChanged();
            }
        }
    }

    public ProcessingMode ProcessingMode
    {
        get => _processingMode;
        set
        {
            if (_processingMode != value)
            {
                _processingMode = value;
                OnPropertyChanged();
                UpdateDisplayedImage();
            }
        }
    }

    public ImageSource? DisplayedImage
    {
        get => _displayedImage;
        set
        {
            if (_displayedImage != value)
            {
                _displayedImage = value;
                OnPropertyChanged();
            }
        }
    }

    public double RotationAngle
    {
        get => _rotationAngle;
        set
        {
            if (_rotationAngle != value)
            {
                _rotationAngle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FinalRotation));
            }
        }
    }

    public double Rotation90Degrees
    {
        get => _rotation90Degrees;
        set
        {
            if (_rotation90Degrees != value)
            {
                _rotation90Degrees = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FinalRotation));
            }
        }
    }

    public double FinalRotation => _rotation90Degrees + _rotationAngle;

    public VisualEffect SelectedVisualEffect
    {
        get => _selectedVisualEffect;
        set
        {
            if (_selectedVisualEffect != value)
            {
                _selectedVisualEffect = value;
                OnPropertyChanged();
                UpdateDisplayedImageForConfirmation();
            }
        }
    }

    public IDrawable? GridDrawable { get; set; }

    public IDrawable? HistogramDrawable { get; set; }

    public ImageGalleryViewModel(IImageService imageService, IImageProcessingService? processingService = null)
    {
        _imageService = imageService;
        _processingService = processingService ?? new ImageProcessingService();
        _imageCache = new ProcessedImageCache(TimeSpan.FromMinutes(5), maxSize: 50);
    }

    public async Task LoadFolderAsync(string folderPath)
    {
        CurrentFolderPath = folderPath;
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();

        IsLoading = true;
        try
        {
            var allImages = await _imageService.ScanFolderAsync(folderPath, _loadingCts.Token);

            _allImages.Clear();
            _allImages.AddRange(allImages);

            // Check if selection folder exists and mark those images as favorites
            var selectionFolder = Path.Combine(folderPath, "selection");
            if (Directory.Exists(selectionFolder))
            {
                var selectionFiles = Directory.GetFiles(selectionFolder);
                // Extract base filenames from processed files (remove suffixes like _WBV, _WBRGB, _CUSTOM, _ORIG)
                var baseFileNamesInSelection = new HashSet<string>();
                foreach (var file in selectionFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                    var extension = Path.GetExtension(file).ToLower();

                    // Remove processing suffix: _WBV, _WBRGB, _CUSTOM, _ORIG
                    var baseName = fileName
                        .Replace("_wbv", "")
                        .Replace("_wbrgb", "")
                        .Replace("_custom", "")
                        .Replace("_orig", "");

                    baseFileNamesInSelection.Add(baseName + extension);
                }

                foreach (var image in _allImages)
                {
                    var originalFileName = Path.GetFileName(image.FilePath).ToLower();
                    if (baseFileNamesInSelection.Contains(originalFileName))
                    {
                        image.IsFavorite = true;
                    }
                }
            }

            // Show all images in the collection view
            _images.Clear();
            foreach (var image in _allImages)
            {
                _images.Add(image);
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Folder loading was cancelled");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void NavigateNext()
    {
    }

    public void NavigatePrevious()
    {
    }

    public void RequestChangeFolder()
    {
        _loadingCts?.Cancel();
        FolderChangeRequested?.Invoke(CurrentFolderPath);
    }

    public void RequestExit()
    {
        _loadingCts?.Cancel();
        ExitRequested?.Invoke();
    }

    /// <summary>
    /// Update the displayed image based on the current processing mode
    /// For processed modes, awaits cached results or triggers computation
    /// </summary>
    private async void UpdateDisplayedImage()
    {
        if (SelectedImage == null || string.IsNullOrEmpty(SelectedImage.FilePath))
        {
            DisplayedImage = null;
            return;
        }

        switch (ProcessingMode)
        {
            case ProcessingMode.Original:
                DisplayedImage = ImageSource.FromFile(SelectedImage.FilePath);
                break;
            case ProcessingMode.WhiteBalanceValue:
                await UpdateProcessedImage(SelectedImage.FilePath, "WhiteBalanceValue");
                break;
            case ProcessingMode.WhiteBalanceRGB:
                await UpdateProcessedImage(SelectedImage.FilePath, "WhiteBalanceRGB");
                break;
        }
    }

    /// <summary>
    /// Try to get a processed image from cache, or wait for background processing
    /// </summary>
    private async Task UpdateProcessedImage(string filePath, string processingType)
    {
        // Try to get from cache first
        var cacheKey = $"{filePath}_{processingType}";
        var cachedData = _imageCache.Get(cacheKey);

        if (cachedData != null)
        {
            IsImageLoading = false;
            DisplayedImage = ImageSource.FromStream(() => new MemoryStream(cachedData));
            return;
        }

        // If processing task is still running, show spinner and wait for it
        if (_currentProcessingTask != null && !_currentProcessingTask.IsCompleted)
        {
            IsImageLoading = true;
            try
            {
                await _currentProcessingTask;

                // Check cache again after processing completes
                cachedData = _imageCache.Get(cacheKey);
                if (cachedData != null)
                {
                    IsImageLoading = false;
                    DisplayedImage = ImageSource.FromStream(() => new MemoryStream(cachedData));
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error waiting for processing: {ex.Message}");
            }
            finally
            {
                IsImageLoading = false;
            }
            return;
        }

        // Fallback: compute synchronously if not in cache
        IsImageLoading = true;
        try
        {
            byte[]? data = null;
            if (processingType == "WhiteBalanceValue")
            {
                data = _processingService.WhiteBalanceValue(filePath);
            }
            else if (processingType == "WhiteBalanceRGB")
            {
                data = _processingService.WhiteBalance(filePath);
            }

            if (data != null && data.Length > 0)
            {
                _imageCache.Set(cacheKey, data);
                IsImageLoading = false;
                DisplayedImage = ImageSource.FromStream(() => new MemoryStream(data));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating {processingType}: {ex.Message}");
            IsImageLoading = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Compute white balance images on demand (when opening the confirmation page)
    /// </summary>
    public async Task ComputeVisualEffectsAsync(string filePath)
    {
        IsComputingVisualEffects = true;
        try
        {
            // Compute both white balance versions
            _temporaryWhiteBalanceValueData = await Task.Run(() => _processingService.WhiteBalanceValue(filePath));
            _temporaryWhiteBalanceRGBData = await Task.Run(() => _processingService.WhiteBalance(filePath));

            // Convert byte arrays to ImageSources for display
            if (_temporaryWhiteBalanceValueData != null && _temporaryWhiteBalanceValueData.Length > 0)
            {
                _visualEffectWBValueImage = ImageSource.FromStream(() => new MemoryStream(_temporaryWhiteBalanceValueData));
            }

            if (_temporaryWhiteBalanceRGBData != null && _temporaryWhiteBalanceRGBData.Length > 0)
            {
                _visualEffectWBRGBImage = ImageSource.FromStream(() => new MemoryStream(_temporaryWhiteBalanceRGBData));
            }

            // Update displayed image if a visual effect is already selected (will remove spinner if image ready)
            UpdateDisplayedImageForConfirmation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error computing visual effects: {ex.Message}");
        }
        finally
        {
            IsComputingVisualEffects = false;
        }
    }

    /// <summary>
    /// Set the original image for the confirmation page (called when modal opens)
    /// </summary>
    public void SetOriginalImageForConfirmation(ImageSource? imageSource)
    {
        _originalImageForConfirmation = imageSource;
        // Reset visual effects when opening confirmation page
        _selectedVisualEffect = VisualEffect.None;
        _visualEffectWBValueImage = null;
        _visualEffectWBRGBImage = null;
        DisplayedImage = imageSource;
    }

    /// <summary>
    /// Clear temporary white balance data after confirmation or cancellation
    /// </summary>
    public void ClearTemporaryVisualEffects()
    {
        _temporaryWhiteBalanceValueData = null;
        _temporaryWhiteBalanceRGBData = null;
        _temporaryCustomData = null;
        _visualEffectWBValueImage = null;
        _visualEffectWBRGBImage = null;
        _visualEffectCustomImage = null;
        SelectedVisualEffect = VisualEffect.None;
    }

    /// <summary>
    /// Update the displayed image based on the selected visual effect
    /// </summary>
    private void UpdateDisplayedImageForConfirmation()
    {
        switch (_selectedVisualEffect)
        {
            case VisualEffect.WhiteBalanceValue:
                if (_visualEffectWBValueImage != null)
                {
                    DisplayedImage = _visualEffectWBValueImage;
                    IsLoadingSelectedVisualEffect = false;
                }
                else
                {
                    // Image not yet computed, still show original but with loading spinner
                    DisplayedImage = _originalImageForConfirmation;
                    IsLoadingSelectedVisualEffect = _isComputingVisualEffects;
                }
                break;
            case VisualEffect.WhiteBalanceRGB:
                if (_visualEffectWBRGBImage != null)
                {
                    DisplayedImage = _visualEffectWBRGBImage;
                    IsLoadingSelectedVisualEffect = false;
                }
                else
                {
                    // Image not yet computed, still show original but with loading spinner
                    DisplayedImage = _originalImageForConfirmation;
                    IsLoadingSelectedVisualEffect = _isComputingVisualEffects;
                }
                break;
            case VisualEffect.Custom:
                if (_visualEffectCustomImage != null)
                {
                    DisplayedImage = _visualEffectCustomImage;
                    IsLoadingSelectedVisualEffect = false;
                }
                else
                {
                    // Custom image not yet computed, show original
                    DisplayedImage = _originalImageForConfirmation;
                    IsLoadingSelectedVisualEffect = false;
                }
                break;
            default:
                // No effect selected, show original without spinner
                DisplayedImage = _originalImageForConfirmation;
                IsLoadingSelectedVisualEffect = false;
                break;
        }
    }

    /// <summary>
    /// Apply custom white balance with current low/high clamp and gamma values
    /// </summary>
    public async Task ApplyCustomWhiteBalanceAsync()
    {
        if (SelectedImage == null || string.IsNullOrEmpty(SelectedImage.FilePath))
            return;

        IsProcessingCustomEffect = true;
        try
        {
            // Compute custom white balance
            _temporaryCustomData = await Task.Run(() =>
                _processingService.WhiteBalanceCustom(SelectedImage.FilePath, _lowClampValue, _highClampValue, _gammaValue));

            if (_temporaryCustomData != null && _temporaryCustomData.Length > 0)
            {
                _visualEffectCustomImage = ImageSource.FromStream(() => new MemoryStream(_temporaryCustomData));

                // Update display to show the custom effect
                DisplayedImage = _visualEffectCustomImage;
                SelectedVisualEffect = VisualEffect.Custom;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying custom white balance: {ex.Message}");
        }
        finally
        {
            IsProcessingCustomEffect = false;
        }
    }

    /// <summary>
    /// Save the selected image with rotation applied and selected visual effect
    /// </summary>
    public async Task SaveSelectedImageWithRotationAsync()
    {
        if (SelectedImage == null || string.IsNullOrEmpty(SelectedImage.FilePath))
            return;

        try
        {
            var originalPath = SelectedImage.FilePath;
            var originalDir = Path.GetDirectoryName(originalPath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);

            // Create selection subfolder
            var selectionFolder = Path.Combine(originalDir, "selection");
            Directory.CreateDirectory(selectionFolder);

            byte[]? imageData = null;
            string outputFileName = "";

            // Get the image data based on the selected visual effect
            if (SelectedVisualEffect == VisualEffect.WhiteBalanceValue && _temporaryWhiteBalanceValueData != null)
            {
                imageData = _temporaryWhiteBalanceValueData;
                outputFileName = $"{fileName}_WBV{extension}";
            }
            else if (SelectedVisualEffect == VisualEffect.WhiteBalanceRGB && _temporaryWhiteBalanceRGBData != null)
            {
                imageData = _temporaryWhiteBalanceRGBData;
                outputFileName = $"{fileName}_WBRGB{extension}";
            }
            else if (SelectedVisualEffect == VisualEffect.Custom && _temporaryCustomData != null)
            {
                imageData = _temporaryCustomData;
                outputFileName = $"{fileName}_CUSTOM{extension}";
            }
            else
            {
                // Default to original
                imageData = await File.ReadAllBytesAsync(originalPath);
                outputFileName = $"{fileName}_ORIG{extension}";
            }

            // Apply rotation to the selected image
            if (imageData != null)
            {
                var tempPath = Path.GetTempFileName();
                try
                {
                    await File.WriteAllBytesAsync(tempPath, imageData);
                    // Only preserve aspect ratio if there's fine-tune rotation
                    bool preserveAspect = _rotationAngle != 0;
                    var rotatedData = _processingService.RotateAndCrop(tempPath, FinalRotation, preserveAspect);

                    // Write the rotated image to the selection folder
                    if (rotatedData != null && rotatedData.Length > 0)
                    {
                        var outputPath = Path.Combine(selectionFolder, outputFileName);
                        await File.WriteAllBytesAsync(outputPath, rotatedData);
                        System.Diagnostics.Debug.WriteLine($"Image saved to: {outputPath}");

                        // Mark the currently selected image as favorite
                        if (SelectedImage != null)
                        {
                            SelectedImage.IsFavorite = true;
                        }
                    }
                }
                finally
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
            throw;
        }
    }
}
