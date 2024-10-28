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

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is Rectangle rect)
        {
            var dialog = new Egorozh.ColorPicker.Dialog.ColorPickerDialog() { Color = (rect.Fill as SolidColorBrush)?.Color ?? Colors.White };
            if (dialog.ShowDialog() == true)
            {
                rect.Fill = new SolidColorBrush(dialog.Color);
            }
        }
    }

    private void SelectFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog()
        {
            DefaultDirectory = _settings.LogFolder
        };

        if (dialog.ShowDialog() == true)
        {
            _settings.LogFolder = dialog.FolderName;
        }
    }

    private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            DialogResult = false;
        }
    }
}
