namespace Pickture.Shared.Behaviors;

public class ImageGalleryKeyboardBehavior : Behavior<ContentPage>
{
    public event EventHandler<KeyEventArgs>? KeyDown;

    protected override void OnAttachedTo(ContentPage page)
    {
        // Keyboard events are handled at the platform level
        // For Windows, we'll add the keyboard handling in the code-behind
        base.OnAttachedTo(page);
    }

    protected override void OnDetachingFrom(ContentPage page)
    {
        base.OnDetachingFrom(page);
    }
}

public class KeyEventArgs : EventArgs
{
    public string? Key { get; set; }
}
