using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace QuizAI.Desktop.Converters;

public class BoolToSelectedBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool selected = value is bool b && b;
        return selected
            ? new SolidColorBrush(Color.Parse("#2980b9"))
            : new SolidColorBrush(Color.Parse("#dfe6e9"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
