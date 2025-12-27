using Pickture.Shared.Models;
using Pickture.Shared.Services;
using Pickture.Features.FolderSelection;

namespace Pickture.Features.ImageGallery;

public partial class ImageGalleryPage : ContentPage, IFileMenuHandler
{
    private readonly ImageGalleryViewModel _viewModel;
    private readonly IImageService _imageService;
    private readonly string _folderPath;

    public ImageGalleryPage(string folderPath)
    {
        InitializeComponent();

        _folderPath = folderPath;
        _imageService = new ImageService();
        _viewModel = new ImageGalleryViewModel(_imageService);
        BindingContext = _viewModel;

        _viewModel.FolderChangeRequested += OnFolderChangeRequested;
        _viewModel.ExitRequested += OnExitRequested;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (string.IsNullOrEmpty(_viewModel.CurrentFolderPath))
        {
            await _viewModel.LoadFolderAsync(_folderPath);
            CenterSelectedThumbnail();
        }

        // Request focus for keyboard input
        this.Focus();
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.RequestChangeFolder();
        return true;
    }

    public void OnChangeFolder()
    {
        _viewModel.RequestChangeFolder();
    }

    public void OnExit()
    {
        _viewModel.RequestExit();
    }

    private void OnThumbnailTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ImageItem imageItem && !string.IsNullOrEmpty(imageItem.FilePath))
        {
            // Find the actual index using file path (most reliable method)
            int actualIndex = _viewModel.FindImageIndexByFilePath(imageItem.FilePath);
            if (actualIndex >= 0)
            {
                _viewModel.SelectedImageIndex = actualIndex;
                CenterSelectedThumbnail();
            }
        }
    }

    private void OnPreviousClicked(object sender, EventArgs e)
    {
        _viewModel.NavigatePrevious();
        CenterSelectedThumbnail();
    }

    private void OnNextClicked(object sender, EventArgs e)
    {
        _viewModel.NavigateNext();
        CenterSelectedThumbnail();
    }

    private void OnFavoriteClicked(object sender, EventArgs e)
    {
        _viewModel.ToggleFavorite();
    }

    private void OnThumbnailCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        // For future virtualization/buffering implementation
    }

    private void CenterSelectedThumbnail()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Get the selected image
            var selectedImage = _viewModel.SelectedImage;
            if (selectedImage == null)
                return;

            // Calculate the scroll position to center the selected item
            // Each thumbnail is 150 pixels tall + 4 pixel spacing = 154 pixels
            const double itemHeight = 154;
            
            // Get the scroll view's visible height
            double scrollViewHeight = ThumbnailScrollView.Height;
            
            // Calculate position: item index * item height - (viewport height / 2 - item height / 2)
            // This centers the item vertically in the visible area
            int selectedIndex = _viewModel.SelectedImageIndex;
            double targetScrollPosition = (selectedIndex * itemHeight) - (scrollViewHeight / 2) + (itemHeight / 2);
            
            // Clamp to valid range
            targetScrollPosition = Math.Max(0, targetScrollPosition);
            
            // Animate scroll to position
            await ThumbnailScrollView.ScrollToAsync(0, targetScrollPosition, true);
        });
    }

    private void OnFolderChangeRequested(string currentFolder)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current!.MainPage = new FolderSelectionPage();
        });
    }

    private void OnExitRequested()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current?.Quit();
        });
    }
}
