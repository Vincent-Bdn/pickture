using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pickture.Shared.Models;
using Pickture.Shared.Services;

namespace Pickture.Features.ImageGallery;

public class ImageGalleryViewModel : INotifyPropertyChanged
{
    private readonly IImageService _imageService;
    private string _currentFolderPath = string.Empty;
    private ImageItem? _selectedImage;
    private int _selectedImageIndex = -1;
    private bool _isLoading;
    private ObservableCollection<ImageItem> _images = new();
    private List<ImageItem> _allImages = new();
    private CancellationTokenSource? _loadingCts;

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

    public ImageGalleryViewModel(IImageService imageService)
    {
        _imageService = imageService;
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

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
