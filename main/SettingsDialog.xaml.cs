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
            // Use solution from https://github.com/DRKV333/ColorPickerWPF
            var prevColor = (rect.Fill as SolidColorBrush)?.Color ?? Colors.White;
            //var dialog = new Egorozh.ColorPicker.Dialog.ColorPickerDialog() { Color = (rect.Fill as SolidColorBrush)?.Color ?? Colors.White };
            //if (dialog.ShowDialog() == true)
            if (ColorPickerWPF.ColorPickerWindow.ShowDialog(out Color newColor, prevColor))
            {
                //rect.Fill = new SolidColorBrush(dialog.Color);
                rect.Fill = new SolidColorBrush(newColor);
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
