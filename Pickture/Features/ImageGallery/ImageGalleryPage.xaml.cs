using Pickture.Shared.Services;

namespace Pickture.Features.ImageGallery;

public partial class ImageGalleryPage : ContentPage
{
    private readonly ImageGalleryViewModel _viewModel;
    private readonly IImageService _imageService;
    private readonly IImageProcessingService _processingService;
    private readonly string _folderPath;

    public ImageGalleryPage(string folderPath)
    {
        InitializeComponent();

        _folderPath = folderPath;
        _imageService = new ImageService();
        _processingService = new ImageProcessingService();
        _viewModel = new ImageGalleryViewModel(_imageService, _processingService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (string.IsNullOrEmpty(_viewModel.CurrentFolderPath))
        {
            await _viewModel.LoadFolderAsync(_folderPath);
            if (_viewModel.Images.Count > 0)
            {
                ThumbnailCollectionView.SelectedItem = _viewModel.SelectedImage;
            }
        }

        Focus();
    }

    private async void OnAddToSelectionClicked(object sender, EventArgs e)
    {
        try
        {
            var confirmationPage = new ImageConfirmationPage(_viewModel);
            await Navigation.PushModalAsync(confirmationPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open confirmation: {ex.Message}", "OK");
        }
    }
}
