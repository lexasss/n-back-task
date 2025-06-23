using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NBackTask;

public partial class SettingsDialog : Window
{
    internal SettingsDialog(Settings settings)
    {
        InitializeComponent();

        _settings = settings;

        DataContext = settings;
    }

    // Internal

    readonly Settings _settings;

    // UI

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is Rectangle rect)
        {
            var currentColor = (rect.Fill as SolidColorBrush)?.Color ?? Colors.White;

            // I couldn't decide which color picker I love more :)
            if (new Random().NextDouble() < 0.5)
            {
                var dialogOptions = ColorPickerWPF.DialogOptions.SimpleView | ColorPickerWPF.DialogOptions.LoadCustomPalette | ColorPickerWPF.DialogOptions.HuePicker;
                if (ColorPickerWPF.ColorPickerWindow.ShowDialog(out Color newColor, currentColor, dialogOptions) == true)
                {
                    rect.Fill = new SolidColorBrush(newColor);
                }
            }
            else // maybe I should get rid of this external dependency...
            {
                var dialog = new Egorozh.ColorPicker.Dialog.ColorPickerDialog() { Color = currentColor };
                if (dialog.ShowDialog() == true)
                {
                    rect.Fill = new SolidColorBrush(dialog.Color);
                }
            }
        }
    }

    private void SelectFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderName = Logger.SelectLogFolder(_settings.LogFolder);
        if (folderName != null)
        {
            _settings.LogFolder = folderName;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
