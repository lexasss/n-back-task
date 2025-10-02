using System.Globalization;
using System.Windows;
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
        string.IsNullOrEmpty((string)value) ? "[not selected yet]" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
}

[ValueConversion(typeof(SessionType), typeof(Visibility))]
public class SessionTypeToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibileFor = (SessionType)parameter;
        return (SessionType)value == visibileFor ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => SessionType.Count;
}

[ValueConversion(typeof(SessionType), typeof(bool))]
public class SessionTypeToEnabled : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var enabledFor = (SessionType)parameter;
        return (SessionType)value == enabledFor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => SessionType.Count;
}

[ValueConversion(typeof(TaskType), typeof(bool))]
public class TaskTypeToEnabled : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var enabledFor = (TaskType)parameter;
        return (TaskType)value == enabledFor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => TaskType.NBack;
}