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

public class ImageGalleryViewModel : INotifyPropertyChanged
{
    private readonly IImageService _imageService;
    private readonly IImageProcessingService _processingService;
    private readonly ProcessedImageCache _imageCache;
    private string _currentFolderPath = string.Empty;
    private ImageItem? _selectedImage;
    private int _selectedImageIndex = -1;
    private bool _isLoading;
    private bool _isImageLoading;
    private ObservableCollection<ImageItem> _images = new();
    private List<ImageItem> _allImages = new();
    private CancellationTokenSource? _loadingCts;
    private CancellationTokenSource? _processingCts;
    private ImageSource? _previewPerChannel;
    private ImageSource? _previewWhiteBalance;
    private ProcessingMode _processingMode = ProcessingMode.Original;
    private ImageSource? _displayedImage;
    private Task? _currentProcessingTask;

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
            if (_selectedImage != value)
            {
                _selectedImage = value;
                OnPropertyChanged();
                
                // Reset to Original mode when selecting a new image
                ProcessingMode = ProcessingMode.Original;
                
                // Update displayed image for the new selection
                if (value != null && !string.IsNullOrEmpty(value.FilePath))
                {
                    UpdateDisplayedImage();
                    
                    // Start background processing of both white balance versions
                    StartBackgroundProcessing(value.FilePath);
                }
            }
        }
    }

    public int SelectedImageIndex
    {
        get => _selectedImageIndex;
        set
        {
            if (_selectedImageIndex != value && value >= 0 && value < _allImages.Count)
            {
                // Clear previous selection
                if (_selectedImageIndex >= 0 && _selectedImageIndex < _allImages.Count)
                {
                    _allImages[_selectedImageIndex].IsSelected = false;
                }
                
                _selectedImageIndex = value;
                SelectedImage = _allImages[value];
                
                // Set new selection
                _allImages[value].IsSelected = true;
                
                OnPropertyChanged();
            }
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

            // Show all images in the collection view
            _images.Clear();
            foreach (var image in _allImages)
            {
                _images.Add(image);
            }

            // Select the first image
            if (_allImages.Count > 0)
            {
                SelectedImageIndex = 0;
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
        if (SelectedImageIndex < _allImages.Count - 1)
        {
            SelectedImageIndex++;
        }
    }

    public void NavigatePrevious()
    {
        if (SelectedImageIndex > 0)
        {
            SelectedImageIndex--;
        }
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

    public void ToggleFavorite()
    {
        if (SelectedImage != null)
        {
            SelectedImage.IsFavorite = !SelectedImage.IsFavorite;
            OnPropertyChanged(nameof(SelectedImage));
        }
    }

    /// <summary>
    /// Find the index of an image item in the full collection
    /// </summary>
    public int FindImageIndex(ImageItem item)
    {
        return _allImages.IndexOf(item);
    }

    /// <summary>
    /// Find the index of an image by file path (more reliable than reference equality)
    /// </summary>
    public int FindImageIndexByFilePath(string filePath)
    {
        for (int i = 0; i < _allImages.Count; i++)
        {
            if (_allImages[i].FilePath == filePath)
                return i;
        }
        return -1;
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

    /// <summary>
    /// Start background processing of both white balance versions
    /// </summary>
    private void StartBackgroundProcessing(string filePath)
    {
        // Cancel any previous processing
        _processingCts?.Cancel();
        _processingCts = new CancellationTokenSource();

        _currentProcessingTask = Task.Run(async () =>
        {
            try
            {
                var cts = _processingCts.Token;

                // Process both versions in parallel
                var task1 = Task.Run(() =>
                {
                    if (!cts.IsCancellationRequested)
                    {
                        var data = _processingService.WhiteBalanceValue(filePath);
                        if (data != null && data.Length > 0)
                        {
                            _imageCache.Set($"{filePath}_WhiteBalanceValue", data);
                        }
                    }
                }, cts);

                var task2 = Task.Run(() =>
                {
                    if (!cts.IsCancellationRequested)
                    {
                        var data = _processingService.WhiteBalance(filePath);
                        if (data != null && data.Length > 0)
                        {
                            _imageCache.Set($"{filePath}_WhiteBalanceRGB", data);
                        }
                    }
                }, cts);

                await Task.WhenAll(task1, task2);
            }
            catch (OperationCanceledException)
            {
                // Processing was cancelled (new image selected)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in background processing: {ex.Message}");
            }
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Save the currently displayed image to the selection subfolder
    /// </summary>
    public async Task SaveSelectedImageAsync()
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

            // Determine output filename
            string outputFileName;
            if (ProcessingMode == ProcessingMode.Original)
            {
                outputFileName = $"{fileName}.orig{extension}";
            }
            else
            {
                outputFileName = $"{fileName}{extension}";
            }

            var outputPath = Path.Combine(selectionFolder, outputFileName);

            // Get the current displayed image data
            byte[]? imageData = null;

            if (ProcessingMode == ProcessingMode.Original)
            {
                // Read original file
                imageData = await File.ReadAllBytesAsync(originalPath);
            }
            else if (ProcessingMode == ProcessingMode.WhiteBalanceValue)
            {
                // Try to get from cache first
                var cacheKey = $"{originalPath}_WhiteBalanceValue";
                imageData = _imageCache.Get(cacheKey);

                // If not in cache, generate it
                if (imageData == null)
                {
                    imageData = _processingService.WhiteBalanceValue(originalPath);
                }
            }
            else if (ProcessingMode == ProcessingMode.WhiteBalanceRGB)
            {
                // Try to get from cache first
                var cacheKey = $"{originalPath}_WhiteBalanceRGB";
                imageData = _imageCache.Get(cacheKey);

                // If not in cache, generate it
                if (imageData == null)
                {
                    imageData = _processingService.WhiteBalance(originalPath);
                }
            }

            // Write the image to the selection folder
            if (imageData != null && imageData.Length > 0)
            {
                await File.WriteAllBytesAsync(outputPath, imageData);
                System.Diagnostics.Debug.WriteLine($"Image saved to: {outputPath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
            throw;
        }
    }
}
