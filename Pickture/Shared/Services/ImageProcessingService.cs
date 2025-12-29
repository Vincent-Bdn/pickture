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

    /// <summary>
    /// Apply custom white balance with user-defined low/high clamping and gamma on V channel.
    /// </summary>
    byte[] WhiteBalanceCustom(string filePath, double lowClamp, double highClamp, double gamma);

    /// <summary>
    /// Rotate an image by the specified angle and optionally crop to preserve aspect ratio.
    /// For pure 90° rotations, set preserveAspectRatio to false to avoid cropping.
    /// </summary>
    byte[] RotateAndCrop(string filePath, double angleInDegrees, bool preserveAspectRatio = true);

    /// <summary>
    /// Rotate a Mat image and optionally crop to preserve aspect ratio.
    /// For pure 90° rotations, set preserveAspectRatio to false to avoid cropping.
    /// </summary>
    Mat RotateAndCrop(Mat image, double angleInDegrees, bool preserveAspectRatio = true);
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

    /// <summary>
    /// Apply custom white balance with user-defined low/high clamping and gamma on V channel.
    /// </summary>
    public byte[] WhiteBalanceCustom(string filePath, double lowClamp, double highClamp, double gamma)
    {
        try
        {
            // Load image
            using var image = Cv2.ImRead(filePath, ImreadModes.Color);
            if (image.Empty())
                return null!;

            // Convert BGR to HSV
            using var hsvImage = new Mat();
            Cv2.CvtColor(image, hsvImage, ColorConversionCodes.BGR2HSV);

            // Split HSV channels
            var channels = hsvImage.Split();
            var vChannel = channels[2]; // V channel is at index 2

            try
            {
                // Apply custom levels to V channel
                // Clamp values: map [lowClamp, highClamp] to [0, 255]
                // Then apply gamma correction
                vChannel.ConvertTo(vChannel, MatType.CV_32F);

                for (int y = 0; y < vChannel.Rows; y++)
                {
                    for (int x = 0; x < vChannel.Cols; x++)
                    {
                        float value = vChannel.Get<float>(y, x);

                        // Clamp to [lowClamp, highClamp] range
                        value = (float)Math.Max(lowClamp, Math.Min(highClamp, value));

                        // Normalize to [0, 1]
                        float normalized = (value - (float)lowClamp) / ((float)highClamp - (float)lowClamp);

                        // Apply gamma correction
                        float gammaCorrect = (float)Math.Pow(normalized, 1.0 / gamma);

                        // Map back to [0, 255]
                        vChannel.Set<float>(y, x, gammaCorrect * 255.0f);
                    }
                }

                vChannel.ConvertTo(vChannel, MatType.CV_8U);

                // Merge channels back
                Cv2.Merge(channels, hsvImage);

                // Convert back to BGR
                using var resultBGR = new Mat();
                Cv2.CvtColor(hsvImage, resultBGR, ColorConversionCodes.HSV2BGR);

                // Encode to PNG
                return resultBGR.ImEncode(".png");
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
            System.Diagnostics.Debug.WriteLine($"Error in WhiteBalanceCustom: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Rotate an image by the specified angle and auto-crop to remove empty borders.
    /// </summary>
    public byte[] RotateAndCrop(string filePath, double angleInDegrees, bool preserveAspectRatio = true)
    {
        try
        {
            using var image = Cv2.ImRead(filePath, ImreadModes.Color);
            if (image.Empty())
                return null!;

            var rotated = RotateAndCrop(image, angleInDegrees, preserveAspectRatio);
            if (rotated == null)
                return null!;

            using var mem = new MemoryStream();
            Cv2.ImEncode(".png", rotated, out var data);
            rotated.Dispose();
            return data;
        }
        catch (Exception)
        {
            return null!;
        }
    }

    /// <summary>
    /// Rotate a Mat image and optionally crop to preserve original aspect ratio.
    /// For pure 90° rotations, set preserveAspectRatio to false to avoid cropping.
    /// </summary>
    public Mat RotateAndCrop(Mat image, double angleInDegrees, bool preserveAspectRatio = true)
    {
        if (image.Empty() || angleInDegrees == 0)
            return image.Clone();

        int height = image.Rows;
        int width = image.Cols;

        // Get rotation matrix (rotating around center, negate angle for correct direction)
        var center = new Point2f(width / 2.0f, height / 2.0f);
        var rotationMatrix = Cv2.GetRotationMatrix2D(center, -angleInDegrees, 1.0);

        // Calculate the bounding box after rotation
        double angleRad = angleInDegrees * Math.PI / 180.0;
        double cosA = Math.Abs(Math.Cos(angleRad));
        double sinA = Math.Abs(Math.Sin(angleRad));

        int newWidth = (int)Math.Round(width * cosA + height * sinA);
        int newHeight = (int)Math.Round(width * sinA + height * cosA);

        // Adjust rotation matrix for the new image size
        rotationMatrix.At<double>(0, 2) += (newWidth - width) / 2.0;
        rotationMatrix.At<double>(1, 2) += (newHeight - height) / 2.0;

        // Apply rotation with white background
        using var rotated = new Mat();
        Cv2.WarpAffine(image, rotated, rotationMatrix, new OpenCvSharp.Size(newWidth, newHeight), 
            InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(255, 255, 255));

        rotationMatrix.Dispose();

        // Only crop if preserving aspect ratio (for fine-tune rotations)
        if (preserveAspectRatio)
        {
            var cropped = CropToOriginalAspectRatio(rotated, width, height, angleInDegrees);
            return cropped;
        }
        else
        {
            // For pure 90° rotations, return the rotated image without cropping
            return rotated.Clone();
        }
    }

    /// <summary>
    /// Crop the rotated image to the largest rectangle that fits within it, preserving the original aspect ratio.
    /// </summary>
    private Mat CropToOriginalAspectRatio(Mat rotatedImage, int originalWidth, int originalHeight, double angleInDegrees)
    {
        if (rotatedImage.Empty())
            return rotatedImage.Clone();

        double angleRad = angleInDegrees * Math.PI / 180.0;
        double cosA = Math.Abs(Math.Cos(angleRad));
        double sinA = Math.Abs(Math.Sin(angleRad));

        // Find the largest axis-aligned rectangle with the same aspect ratio as the original
        // that fits inside the rotated image bounds.
        // 
        // The rotated image bounds are:
        //   rotated_width = W*|cos(θ)| + H*|sin(θ)|
        //   rotated_height = W*|sin(θ)| + H*|cos(θ)|
        //
        // We need to find scaling factor s such that:
        //   s <= |cos(θ)| + (1/r)*|sin(θ)|
        //   s <= r*|sin(θ)| + |cos(θ)|
        // where r = W/H is the original aspect ratio.

        double r = (double)originalWidth / originalHeight;
        
        // Calculate the two constraints on the scaling factor
        // s must satisfy: s <= |cos(θ)| + (1/r)|sin(θ)| AND s <= r|sin(θ)| + |cos(θ)|
        // To find the maximum s, we take the minimum of the right-hand sides
        // But since we're fitting a smaller rectangle inside the rotated bounds,
        // we need the reciprocal: s = 1 / max(constraint1, constraint2)
        double constraint1 = cosA + (1.0 / r) * sinA;
        double constraint2 = r * sinA + cosA;
        
        // The scaling factor is the reciprocal of the maximum constraint
        double s = 1.0 / Math.Max(constraint1, constraint2);
        
        // Calculate the crop dimensions
        double cropW = s * originalWidth;
        double cropH = s * originalHeight;

        // Get the rotated image dimensions for centering
        double rotatedW = rotatedImage.Cols;
        double rotatedH = rotatedImage.Rows;

        // Center the crop rectangle
        int x = (int)((rotatedW - cropW) / 2.0);
        int y = (int)((rotatedH - cropH) / 2.0);
        int w = (int)cropW;
        int h = (int)cropH;

        // Ensure valid rectangle
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x + w > rotatedImage.Cols) w = rotatedImage.Cols - x;
        if (y + h > rotatedImage.Rows) h = rotatedImage.Rows - y;

        if (w <= 0 || h <= 0)
            return rotatedImage.Clone();

        var rect = new OpenCvSharp.Rect(x, y, w, h);
        using var roi = new Mat(rotatedImage, rect);
        return roi.Clone();
    }
}
