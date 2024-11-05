using ColorPickerWPF.Code;
using System;
using System.Windows;
using System.Windows.Media;

namespace ColorPickerWPF;

public partial class ColorPickerWindow : Window
{
    protected readonly int WidthMax = 574;
    protected readonly int WidthMin = 342;
    protected bool SimpleMode { get; set; }

    public ColorPickerWindow()
    {
        InitializeComponent();
    }
    
    public static bool ShowDialog(
        out Color color, Color? seedColor = null, ColorPickerDialogOptions flags = ColorPickerDialogOptions.None,
        ColorPickerControl.ColorPickerChangeHandler customPreviewEventHandler = null, Action<ColorPickerWindow> customiseWindow = null)
    {
        if ((flags & ColorPickerDialogOptions.LoadCustomPalette) == ColorPickerDialogOptions.LoadCustomPalette)
        {
            ColorPickerSettings.UsingCustomPalette = true;
        }

        var instance = new ColorPickerWindow();
        instance.ColorPicker.SetColor(seedColor ?? Colors.White);
        color = instance.ColorPicker.Color;

        customiseWindow?.Invoke(instance);

        if ((flags & ColorPickerDialogOptions.SimpleView) == ColorPickerDialogOptions.SimpleView)
        {
            instance.ToggleSimpleAdvancedView();
        }

        if (ColorPickerSettings.UsingCustomPalette)
        {
            instance.ColorPicker.LoadDefaultCustomPalette();
        }

        if (customPreviewEventHandler != null)
        {
            instance.ColorPicker.OnPickColor += customPreviewEventHandler;
        }

        var result = instance.ShowDialog();
        if (result.HasValue && result.Value)
        {
            color = instance.ColorPicker.Color;
            return true;
        }

        return false;
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Hide();
    }

    private void MinMaxViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (SimpleMode)
        {
            SimpleMode = false;
            MinMaxViewButton.Content = "<< Simple";
            Width = WidthMax;
        }
        else
        {
            SimpleMode = true;
            MinMaxViewButton.Content = "Advanced >>";
            Width = WidthMin;
        }
    }

    public void ToggleSimpleAdvancedView()
    {
        if (SimpleMode)
        {
            SimpleMode = false;
            MinMaxViewButton.Content = "<< Simple";
            Width = WidthMax;
        }
        else
        {
            SimpleMode = true;
            MinMaxViewButton.Content = "Advanced >>";
            Width = WidthMin;
        }
    }
}