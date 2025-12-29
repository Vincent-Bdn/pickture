using Pickture.Features.ImageGallery.Components;

namespace Pickture.Features.ImageGallery;

public partial class ImageConfirmationPage : ContentPage
{
    private readonly ImageGalleryViewModel _viewModel;
    private EventHandler<TextChangedEventArgs>? _lowClampTextChangedHandler;
    private EventHandler<TextChangedEventArgs>? _highClampTextChangedHandler;
    private IDispatcherTimer? _histogramRefreshTimer;

    public ImageConfirmationPage(ImageGalleryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Set the grid drawable
        _viewModel.GridDrawable = new GridDrawable();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Create event handlers that can be properly unsubscribed
        _lowClampTextChangedHandler = (s, e) => UpdateHistogramBars();
        _highClampTextChangedHandler = (s, e) => UpdateHistogramBars();

        // Subscribe to Entry field changes for real-time histogram updates
        if (LowClampEntry != null)
            LowClampEntry.TextChanged += _lowClampTextChangedHandler;

        if (HighClampEntry != null)
            HighClampEntry.TextChanged += _highClampTextChangedHandler;

        // Start a timer to refresh histogram every 5 seconds as a fallback
        _histogramRefreshTimer = Dispatcher.CreateTimer();
        _histogramRefreshTimer.Interval = TimeSpan.FromSeconds(5);
        _histogramRefreshTimer.Tick += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[TIMER] 5-second refresh triggered - Low: {_viewModel.LowClampValue}, High: {_viewModel.HighClampValue}");
            UpdateHistogramBars();
        };
        _histogramRefreshTimer.Start();
        System.Diagnostics.Debug.WriteLine("[TIMER] Histogram refresh timer started");

        // Set the original image reference
        _viewModel.SetOriginalImageForConfirmation(_viewModel.DisplayedImage);

        // Compute white balance images on demand
        if (_viewModel.SelectedImage != null && !string.IsNullOrEmpty(_viewModel.SelectedImage.FilePath))
        {
            await _viewModel.ComputeVisualEffectsAsync(_viewModel.SelectedImage.FilePath);

            // Generate histogram for custom effect display
            await GenerateHistogramAsync(_viewModel.SelectedImage.FilePath);
        }
    }

    private void UpdateHistogramBars()
    {
        // Update the histogram drawable with current form values
        if (HistogramView?.Drawable is HistogramDrawable hist)
        {
            hist.LowClampValue = (int)_viewModel.LowClampValue;
            hist.HighClampValue = (int)_viewModel.HighClampValue;

            // Redraw histogram immediately
            HistogramView.Invalidate();
        }
    }

    private async Task GenerateHistogramAsync(string imagePath)
    {
        try
        {
            _viewModel.IsLoadingHistogram = true;
            var drawable = await Task.Run(() =>
            {
                using var mat = OpenCvSharp.Cv2.ImRead(imagePath, OpenCvSharp.ImreadModes.Color);
                if (mat.Empty()) return null;

                // Convert to HSV
                using var hsv = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.CvtColor(mat, hsv, OpenCvSharp.ColorConversionCodes.BGR2HSV);

                // Extract V (brightness) channel
                using var vChannel = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.ExtractChannel(hsv, vChannel, 2);

                // Calculate histogram
                int[] histogram = new int[256];
                for (int y = 0; y < vChannel.Rows; y++)
                {
                    for (int x = 0; x < vChannel.Cols; x++)
                    {
                        byte value = vChannel.Get<byte>(y, x);
                        histogram[value]++;
                    }
                }

                return new HistogramDrawable
                {
                    BrightnessHistogram = histogram,
                    HistogramBinCount = 256,
                    LowClampValue = (int)_viewModel.LowClampValue,
                    HighClampValue = (int)_viewModel.HighClampValue
                };
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _viewModel.HistogramDrawable = drawable;
                HistogramView.Drawable = drawable;
                HistogramView.Invalidate();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating histogram: {ex.Message}");
        }
        finally
        {
            _viewModel.IsLoadingHistogram = false;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Stop the timer
        if (_histogramRefreshTimer != null)
        {
            _histogramRefreshTimer.Stop();
            System.Diagnostics.Debug.WriteLine("[TIMER] Histogram refresh timer stopped");
        }

        // Unsubscribe from Entry field changes
        if (LowClampEntry != null && _lowClampTextChangedHandler != null)
            LowClampEntry.TextChanged -= _lowClampTextChangedHandler;
        if (HighClampEntry != null && _highClampTextChangedHandler != null)
            HighClampEntry.TextChanged -= _highClampTextChangedHandler;

        _viewModel.ClearTemporaryVisualEffects();
        await Navigation.PopModalAsync();
    }

    private void OnRotateLeft90Clicked(object sender, EventArgs e)
    {
        _viewModel.Rotation90Degrees = (_viewModel.Rotation90Degrees - 90) % 360;
        if (_viewModel.Rotation90Degrees < 0)
            _viewModel.Rotation90Degrees += 360;
    }

    private void OnRotateRight90Clicked(object sender, EventArgs e)
    {
        _viewModel.Rotation90Degrees = (_viewModel.Rotation90Degrees + 90) % 360;
    }

    private void OnResetRotationClicked(object sender, EventArgs e)
    {
        _viewModel.RotationAngle = 0;
    }

    private async void OnVisualEffectClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string effectStr)
        {
            if (Enum.TryParse<VisualEffect>(effectStr, out var effect))
            {
                // Toggle: if already selected, deselect; otherwise select
                _viewModel.SelectedVisualEffect = _viewModel.SelectedVisualEffect == effect ? VisualEffect.None : effect;
            }
        }
    }

    private async void OnApplyCustomEffectClicked(object sender, EventArgs e)
    {
        try
        {
            // Ensure Custom effect is selected
            _viewModel.SelectedVisualEffect = VisualEffect.Custom;

            // Apply the custom white balance with current slider values
            await _viewModel.ApplyCustomWhiteBalanceAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to apply custom effect: {ex.Message}", "OK");
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        try
        {
            // Stop the timer
            if (_histogramRefreshTimer != null)
            {
                _histogramRefreshTimer.Stop();
                System.Diagnostics.Debug.WriteLine("[TIMER] Histogram refresh timer stopped");
            }

            // Unsubscribe from Entry field changes
            if (LowClampEntry != null && _lowClampTextChangedHandler != null)
                LowClampEntry.TextChanged -= _lowClampTextChangedHandler;
            if (HighClampEntry != null && _highClampTextChangedHandler != null)
                HighClampEntry.TextChanged -= _highClampTextChangedHandler;

            await _viewModel.SaveSelectedImageWithRotationAsync();
            _viewModel.ClearTemporaryVisualEffects();
            await DisplayAlert("Success", "Image added to selection", "OK");
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save image: {ex.Message}", "OK");
        }
    }
}
