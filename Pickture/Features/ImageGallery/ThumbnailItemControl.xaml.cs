using Pickture.Shared.Models;

namespace Pickture.Features.ImageGallery;

public partial class ThumbnailItemControl : ContentView
{
    public static readonly BindableProperty ImageItemProperty =
        BindableProperty.Create(
            nameof(ImageItem),
            typeof(ImageItem),
            typeof(ThumbnailItemControl),
            null,
            propertyChanged: OnImageItemChanged);

    public ImageItem? ImageItem
    {
        get => (ImageItem?)GetValue(ImageItemProperty);
        set => SetValue(ImageItemProperty, value);
    }

    public ThumbnailItemControl()
    {
        InitializeComponent();
    }

    private static void OnImageItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ThumbnailItemControl)bindable;
        var imageItem = (ImageItem?)newValue;

        if (imageItem != null)
        {
            // Binding will handle setting the FilePath and FileName
            // AsyncThumbnailImage will load the thumbnail automatically
            control.BindingContext = imageItem;
        }
    }
}
