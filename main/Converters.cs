using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NBackTask;

[ValueConversion(typeof(SolidColorBrush), typeof(Brush))]
public class ColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (Brush)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (SolidColorBrush)value;
}

[ValueConversion(typeof(bool), typeof(bool))]
public class NegateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (bool)value == false;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (bool)value == false;
}

[ValueConversion(typeof(string), typeof(string))]
public class PathUIConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty((string)value) ? "[not yet selected]" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
}