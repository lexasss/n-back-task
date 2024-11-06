using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;

namespace ColorPickerWPF;

public partial class ColorPickerSwatch : UserControl
{
    public static ColorPickerControl ColorPickerControl { get; set; }

    public event EventHandler<Color> ColorPicked;

    public bool Editable { get; set; }
    public Color CurrentColor { get; set; } = Colors.White;

    public ColorPickerSwatch()
    {
        InitializeComponent();
    }

    internal ColorSwatchItem[] GetColors()
    {
        if (SwatchListBox.ItemsSource is ColorSwatchItem[] colors)
        {
            return colors;
        }

        return [];
    }

    // Internal

    private void UIElement_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (Editable && Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            border.Background = new SolidColorBrush(CurrentColor);

            if (border.DataContext is ColorSwatchItem data)
            {
                data.Color = CurrentColor;
                data.HexString = CurrentColor.ToHexString();
            }

            ColorPickerControl?.CustomColorsChanged();
        }
        else
        {
            var color = border.Background as SolidColorBrush;
            ColorPicked?.Invoke(this, color.Color);
        }
    }
}