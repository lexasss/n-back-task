using ColorPickerWPF.Code;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;

namespace ColorPickerWPF;

public partial class ColorPickerSwatch : UserControl
{
    public delegate void ColorSwatchPickHandler(Color color);

    public static ColorPickerControl ColorPickerControl { get; set; }

    public event ColorSwatchPickHandler OnPickColor;

    public bool Editable { get; set; }
    public Color CurrentColor = Colors.White;

    public ColorPickerSwatch()
    {
        InitializeComponent();
    }

    private void UIElement_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var border = (sender as Border);
        if (border == null)
            return;

        if (Editable && Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            border.Background = new SolidColorBrush(CurrentColor);

            var data = border.DataContext as ColorSwatchItem;
            if (data != null)
            {
                data.Color = CurrentColor;
                data.HexString = CurrentColor.ToHexString();
            }

            if (ColorPickerControl != null)
            {
                ColorPickerControl.CustomColorsChanged();
            }
        }
        else
        {
            var color = border.Background as SolidColorBrush;
            OnPickColor?.Invoke(color.Color);
        }
    }

    internal List<ColorSwatchItem> GetColors()
    {
        var results = new List<ColorSwatchItem>();

        var colors = SwatchListBox.ItemsSource as List<ColorSwatchItem>;
        if (colors != null)
        {
            return colors;
        }

        return results;
    }
}