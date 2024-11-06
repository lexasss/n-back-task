using System;
using System.Windows;
using System.Windows.Media;

namespace ColorPickerWPF;

public partial class ColorPickerWindow : Window
{
    public ColorPickerWindow()
    {
        InitializeComponent();
    }
    
    public static bool? ShowDialog(out Color color,
        Color? seedColor = null,
        DialogOptions flags = DialogOptions.None,
        EventHandler<Color> colorPreview = null,
        Action<ColorPickerWindow> customiseWindow = null)
    {
        if ((flags & DialogOptions.LoadCustomPalette) == DialogOptions.LoadCustomPalette)
        {
            ColorPickerSettings.UsingCustomPalette = true;
        }

        var instance = new ColorPickerWindow();
        instance.ColorPicker.SetColor(seedColor ?? Colors.White);
        color = instance.ColorPicker.Color;

        customiseWindow?.Invoke(instance);

        if ((flags & DialogOptions.SimpleView) == DialogOptions.SimpleView)
        {
            instance.ToggleSimpleAdvancedView();
        }

        if ((flags & DialogOptions.HuePicker) == DialogOptions.HuePicker)
        {
            instance.ColorPicker.ShowHueTab();
        }

        if (ColorPickerSettings.UsingCustomPalette)
        {
            instance.ColorPicker.LoadDefaultCustomPalette();
        }

        if (colorPreview != null)
        {
            instance.ColorPicker.ColorPicked += colorPreview;
        }

        var result = instance.ShowDialog();
        if (result == true)
        {
            color = instance.ColorPicker.Color;
        }

        return result;
    }

    // Internal

    const int WidthMax = 574;
    const int WidthMin = 350;

    bool _simpleMode;

    private void ToggleSimpleAdvancedView()
    {
        if (_simpleMode)
        {
            _simpleMode = false;
            btnMinMaxView.Content = "<< Simple";
            Width = WidthMax;
        }
        else
        {
            _simpleMode = true;
            btnMinMaxView.Content = "Advanced >>";
            Width = WidthMin;
        }
    }

    // UI

    private void OK_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void MinMaxView_Click(object sender, RoutedEventArgs e) => ToggleSimpleAdvancedView();
}