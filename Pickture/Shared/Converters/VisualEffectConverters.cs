using System.Globalization;

namespace Pickture.Shared.Converters;

public class VisualEffectButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value == null)
            return Colors.White;

        var selectedEffect = (Pickture.Features.ImageGallery.VisualEffect?)value;
        var parameterEffect = parameter.ToString();

        if (Enum.TryParse<Pickture.Features.ImageGallery.VisualEffect>(parameterEffect, out var effect))
        {
            // Selected: blue, Unselected: light grey
            return selectedEffect == effect ? Color.FromArgb("#007AFF") : Color.FromArgb("#E0E0E0");
        }

        return Color.FromArgb("#E0E0E0");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class VisualEffectTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value == null)
            return Colors.Black;

        var selectedEffect = (Pickture.Features.ImageGallery.VisualEffect?)value;
        var parameterEffect = parameter.ToString();

        if (Enum.TryParse<Pickture.Features.ImageGallery.VisualEffect>(parameterEffect, out var effect))
        {
            // Selected: white, Unselected: dark grey
            return selectedEffect == effect ? Colors.White : Color.FromArgb("#333333");
        }

        return Color.FromArgb("#333333");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class CustomEffectVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return false;

        var selectedEffect = (Pickture.Features.ImageGallery.VisualEffect?)value;
        return selectedEffect == Pickture.Features.ImageGallery.VisualEffect.Custom;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
