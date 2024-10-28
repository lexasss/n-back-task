using System.Windows.Media;

namespace NBackTask;

internal static class StringExt
{
    public static string ToPath(this string s, string replacement = "-")
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        string[] temp = s.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(replacement, temp);
    }
}

internal static class SolidColorBrushExt
{
    public static string Serialize(this SolidColorBrush brush) => brush.Color.ToString();

    public static SolidColorBrush Deserialize(this string s)
    {
        if (s.StartsWith('#') && s.Length == 9)
        {
            byte a = byte.Parse(s[1..3], System.Globalization.NumberStyles.HexNumber);
            byte r = byte.Parse(s[3..5], System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(s[5..7], System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(s[7..], System.Globalization.NumberStyles.HexNumber);
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }
        else if (_knownColors.TryGetValue(s, out Color color))
        {
            return new SolidColorBrush(color);
        }
        else
        {
            return Brushes.Black; // rollback
        }
    }

    static readonly Dictionary<string, Color> _knownColors = typeof(Colors)
            .GetProperties()
            .Where(prop => prop.PropertyType == typeof(Color))
            .ToDictionary(prop => prop.Name,
                          prop => (Color)prop.GetValue(null));
}
