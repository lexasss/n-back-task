using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CttTest;

[ValueConversion(typeof(SolidColorBrush), typeof(Brush))]
public class ColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Brush)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (SolidColorBrush)value;
    }
}

[ValueConversion(typeof(bool), typeof(bool))]
public class NegateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value == false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value == false;
    }
}