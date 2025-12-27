namespace Pickture.Features.FolderSelection;

public partial class FolderSelectionPage : ContentPage
{
    private readonly FolderSelectionViewModel _viewModel;

    public FolderSelectionPage()
    {
        InitializeComponent();
        _viewModel = new FolderSelectionViewModel();
        BindingContext = _viewModel;

        _viewModel.FolderSelected += OnFolderSelected;
    }

    private async void OnLoadFolderClicked(object sender, EventArgs e)
    {
        LoadFolderButton.IsEnabled = false;
        try
        {
            await _viewModel.SelectFolderAsync();
        }
        finally
        {
            LoadFolderButton.IsEnabled = true;
        }
    }

    private void OnFolderSelected(string folderPath)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current!.MainPage = new ImageGallery.ImageGalleryPage(folderPath);
        });
    }
}
