using System.Globalization;

namespace Pickture.Shared.Converters;

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }
}

public class ThumbnailConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Thumbnails are now handled by AsyncThumbnailImage custom control
        // This converter is kept for compatibility but not actively used
        return null!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FullImageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && File.Exists(filePath))
        {
            return ImageSource.FromFile(filePath);
        }
        return null!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectionColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Colors.Blue; // Blue border for selected item
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SelectionWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return 3.0; // 3px border width for selected item
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ProcessingModeButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Color.FromArgb("#D0D0D0"); // Default gray
        
        // value is the current ProcessingMode, parameter is the button's mode
        var currentMode = value.ToString();
        var buttonMode = parameter.ToString();
        
        // Highlight selected button in blue
        if (currentMode == buttonMode)
        {
            return Color.FromArgb("#007AFF"); // Bright blue for selected
        }
        
        return Color.FromArgb("#D0D0D0"); // Gray for unselected
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ProcessingModeTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Color.FromArgb("#000000"); // Default black
        
        // value is the current ProcessingMode, parameter is the button's mode
        var currentMode = value.ToString();
        var buttonMode = parameter.ToString();
        
        // White text when selected (blue button), black text when unselected
        if (currentMode == buttonMode)
        {
            return Color.FromArgb("#FFFFFF"); // White text for selected
        }
        
        return Color.FromArgb("#000000"); // Black text for unselected
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
