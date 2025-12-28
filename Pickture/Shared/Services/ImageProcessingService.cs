using OpenCvSharp;

namespace Pickture.Shared.Services;

/// <summary>
/// Service for processing images with various enhancement techniques
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Apply GIMP's White Balance to the Value channel only (HSV).
    /// This preserves hue and saturation while adjusting brightness.
    /// </summary>
    byte[] WhiteBalanceValue(string filePath, double discard = 0.05);

    /// <summary>
    /// Apply GIMP's White Balance (auto white balance by stretching each channel separately)
    /// </summary>
    byte[] WhiteBalance(string filePath, double discard = 0.05);
}

public class ImageProcessingService : IImageProcessingService
{
    /// <summary>
    /// Apply GIMP's Levels adjustment to the Value (brightness) channel only.
    /// Uses GIMP's levels algorithm with automatic low/high input detection.
    /// This adjusts brightness while preserving colors (no hue/saturation shifts).
    /// </summary>
    public byte[] WhiteBalanceValue(string filePath, double discard = 0.05)
    {
        try
        {
            // Load image
            using var image = Cv2.ImRead(filePath, ImreadModes.Color);
            if (image.Empty())
                return null!;

            // Convert BGR to HSV
            using var hsv = new Mat();
            Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

            // Split into HSV channels
            var channels = hsv.Split();

            try
            {
                // Apply levels to the Value (V) channel (index 2)
                ApplyLevelsToChannel(channels[2], gamma: 1.15, lowOutput: 0, highOutput: 255);

                // Merge channels back
                using var result = new Mat();
                Cv2.Merge(channels, result);

                // Convert back to BGR
                Cv2.CvtColor(result, result, ColorConversionCodes.HSV2BGR);

                // Encode to PNG
                return result.ImEncode(".png");
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in WhiteBalanceValue: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Apply GIMP's White Balance algorithm:
    /// Stretches each RGB channel independently by discarding outlier pixels.
    /// </summary>
    public byte[] WhiteBalance(string filePath, double discard = 0.05)
    {
        try
        {
            // Load image
            using var image = Cv2.ImRead(filePath, ImreadModes.Color);
            if (image.Empty())
                return null!;

            // Split into BGR channels
            var channels = image.Split();

            try
            {
                // Process each channel
                for (int i = 0; i < channels.Length; i++)
                {
                    ApplyWhiteBalanceToChannel(channels[i], discard);
                }

                // Merge channels back
                using var result = new Mat();
                Cv2.Merge(channels, result);

                // Encode to PNG
                return result.ImEncode(".png");
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in WhiteBalance: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Apply white balance to a single channel by discarding outliers and stretching
    /// </summary>
    private void ApplyWhiteBalanceToChannel(Mat channel, double discard = 0.05)
    {
        // Calculate histogram
        var hist = new Mat();
        int[] histSize = { 256 };
        Rangef[] ranges = { new Rangef(0, 256) };
        Cv2.CalcHist(new[] { channel }, new[] { 0 }, null, hist, 1, histSize, ranges);

        // Count total pixels
        long totalPixels = channel.Width * channel.Height;

        // Find min value (discard clampInput % from bottom)
        long pixelCount = 0;
        double thresholdPixels = totalPixels * (discard / 100.0);
        byte minValue = 0;
        for (int i = 0; i < 256; i++)
        {
            float histValue = hist.Get<float>(i);
            pixelCount += (long)histValue;
            if (pixelCount >= thresholdPixels)
            {
                minValue = (byte)i;
                break;
            }
        }

        // Find max value (discard clampInput % from top)
        pixelCount = 0;
        byte maxValue = 255;
        for (int i = 255; i >= 0; i--)
        {
            float histValue = hist.Get<float>(i);
            pixelCount += (long)histValue;
            if (pixelCount >= thresholdPixels)
            {
                maxValue = (byte)i;
                break;
            }
        }

        hist.Dispose();

        // If range is too small or invalid, don't stretch
        if (maxValue <= minValue)
            return;

        // Apply linear stretch: map [minValue, maxValue] to [0, 255]
        channel.ConvertTo(channel, MatType.CV_32F);
        Cv2.Subtract(channel, new Scalar(minValue), channel);
        Cv2.Multiply(channel, new Scalar(255.0 / (maxValue - minValue)), channel);
        Cv2.Min(channel, 255, channel); // Clamp to 255
        Cv2.Max(channel, 0, channel);   // Clamp to 0
        channel.ConvertTo(channel, MatType.CV_8U);
    }

    /// <summary>
    /// Apply GIMP's Levels adjustment to a single channel.
    /// Automatically detects low/high input values (lowest and highest non-zero values).
    /// Then applies gamma correction and output level mapping.
    /// </summary>
    private void ApplyLevelsToChannel(Mat channel, double gamma = 1.0, int lowOutput = 0, int highOutput = 255)
    {
        // Find the lowest and highest non-zero values in the channel
        byte lowInput = 255;
        byte highInput = 0;
        
        // Scan the channel to find min and max non-zero values
        for (int y = 0; y < channel.Rows; y++)
        {
            for (int x = 0; x < channel.Cols; x++)
            {
                byte value = channel.At<byte>(y, x);
                if (value > 0)
                {
                    if (value < lowInput)
                        lowInput = value;
                    if (value > highInput)
                        highInput = value;
                }
            }
        }

        // If all pixels are 0 or no variation found, return unchanged
        if (lowInput >= highInput)
            return;

        // Apply levels transformation with gamma correction
        channel.ConvertTo(channel, MatType.CV_32F, 1.0 / 255.0); // Normalize to [0, 1]
        
        for (int y = 0; y < channel.Rows; y++)
        {
            for (int x = 0; x < channel.Cols; x++)
            {
                float value = channel.At<float>(y, x);
                
                // Map from input range [lowInput/255, highInput/255] to [0, 1]
                float normalized = (value - (lowInput / 255.0f)) / ((highInput - lowInput) / 255.0f);
                
                // Clamp to [0, 1] if clamp_input is true
                normalized = Math.Max(0, Math.Min(1, normalized));
                
                // Apply gamma correction
                float gamma_corrected = (float)Math.Pow(normalized, 1.0 / gamma);
                
                // Map to output range [lowOutput, highOutput]
                float output = gamma_corrected * (highOutput - lowOutput) + lowOutput;
                
                // Clamp to output range if clamp_output is true
                output = Math.Max(lowOutput, Math.Min(highOutput, output));
                
                channel.Set<float>(y, x, output / 255.0f); // Keep in normalized form
            }
        }
        
        channel.ConvertTo(channel, MatType.CV_8U, 255.0); // Convert back to [0, 255]
    }
}
