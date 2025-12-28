namespace Pickture.Features.ImageGallery;

public partial class ImageConfirmationPage : ContentPage
{
    private readonly ImageGalleryViewModel _viewModel;

    public ImageConfirmationPage(ImageGalleryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        try
        {
            await _viewModel.SaveSelectedImageAsync();
            await DisplayAlert("Success", "Image added to selection", "OK");
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save image: {ex.Message}", "OK");
        }
    }
}
