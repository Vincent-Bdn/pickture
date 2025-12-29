namespace Pickture.Features.ImageGallery.Components;

public class GridDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var width = dirtyRect.Width;
        var height = dirtyRect.Height;

        canvas.StrokeColor = Color.FromArgb("#555555"); // Dark grey
        canvas.StrokeSize = 1;

        // Draw 5x5 grid
        for (int i = 1; i < 5; i++)
        {
            // Vertical lines
            var x = (width / 5) * i;
            canvas.DrawLine(x, 0, x, height);

            // Horizontal lines
            var y = (height / 5) * i;
            canvas.DrawLine(0, y, width, y);
        }
    }
}
