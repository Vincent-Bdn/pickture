namespace Pickture.Features.ImageGallery.Components;

/// <summary>
/// Drawable for rendering histogram visualization with draggable range selection bars
/// </summary>
public class HistogramDrawable : IDrawable
{
    public int[]? BrightnessHistogram { get; set; }
    public int MaxBrightness { get; set; } = 255;
    public int HistogramBinCount { get; set; } = 256;
    public int LowClampValue { get; set; } = 0;
    public int HighClampValue { get; set; } = 255;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BrightnessHistogram == null || BrightnessHistogram.Length == 0)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            return;
        }

        var width = dirtyRect.Width;
        var height = dirtyRect.Height;
        
        // Margins for axes and labels
        float leftMargin = 0;
        float bottomMargin = 0;
        float topMargin = 0;
        float rightMargin = 0;
        
        float chartWidth = width - leftMargin - rightMargin;
        float chartHeight = height - topMargin - bottomMargin;

        // Draw white background
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, width, height);

        // Draw chart border
        canvas.StrokeColor = Color.FromArgb("#999999");
        canvas.StrokeSize = 1;

        // Find max value for scaling
        int maxCount = BrightnessHistogram.Max();
        if (maxCount == 0) maxCount = 1;

        // Draw histogram bars
        float barWidth = chartWidth / HistogramBinCount;
        canvas.FillColor = Color.FromArgb("#007AFF");

        for (int i = 0; i < BrightnessHistogram.Length && i < HistogramBinCount; i++)
        {
            int count = BrightnessHistogram[i];
            if (count > 0)
            {
                float barHeight = (count / (float)maxCount) * chartHeight;
                float x = leftMargin + (i * barWidth);
                float y = topMargin + chartHeight - barHeight;

                canvas.FillRectangle(x, y, barWidth, barHeight);
            }
        }

        // Draw X axis (0-255)
        canvas.StrokeColor = Color.FromArgb("#333333");
        canvas.StrokeSize = 1;
        canvas.DrawLine(leftMargin, topMargin + chartHeight, leftMargin + chartWidth, topMargin + chartHeight);

        // Draw range selection bars
        // Draw semi-transparent shade for values outside range
        canvas.FillColor = Color.FromArgb("#00000020"); // Very faint black overlay
        
        // Shade below low value
        float lowBarX = leftMargin + (LowClampValue * barWidth);
        canvas.FillRectangle(leftMargin, topMargin, lowBarX - leftMargin, chartHeight);
        
        // Shade above high value
        float highBarX = leftMargin + (HighClampValue * barWidth);
        canvas.FillRectangle(highBarX, topMargin, leftMargin + chartWidth - highBarX, chartHeight);
        
        // Draw low value bar (left edge)
        canvas.StrokeColor = Color.FromArgb("#FF3B30"); // Red for low
        canvas.StrokeSize = 2;
        canvas.DrawLine(lowBarX, topMargin, lowBarX, topMargin + chartHeight);
        
        // Draw high value bar (right edge)
        canvas.StrokeColor = Color.FromArgb("#34C759"); // Green for high
        canvas.StrokeSize = 2;
        canvas.DrawLine(highBarX, topMargin, highBarX, topMargin + chartHeight);
    }
}
