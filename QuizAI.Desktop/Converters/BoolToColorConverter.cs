using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace QuizAI.Desktop.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.Parse("#e74c3c"));
        return new SolidColorBrush(Color.Parse("#95a5a6"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
