using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NBackTask;

public partial class SettingsDialog : Window, INotifyPropertyChanged
{
    public Settings Settings { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal SettingsDialog(Settings settings)
    {
        InitializeComponent();

        Settings = settings;
        if (!string.IsNullOrEmpty(Settings.Name))
        {
            Title += $" - {Settings.Name}";
        }

        DataContext = settings;
    }

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
        var folderName = Logger.SelectLogFolder(Settings.LogFolder);
        if (folderName != null)
        {
            Settings.LogFolder = folderName;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var menu = ContextMenuService.GetContextMenu(btn);
            if (menu?.IsOpen == false)
            {
                menu.HorizontalOffset = btn.ActualWidth + 5;
                menu.VerticalOffset = btn.ActualHeight;
                menu.PlacementTarget = btn;
                menu.IsOpen = true;
            }
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var profileName = InputDialog.ShowDialog("Enter the profile name:");
        if (profileName != null)
        {
            Settings.Name = profileName;
            DialogResult = true;
        }
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Profiles();
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedProfile))
        {
            var profile = dialog.SelectedProfile;

            var settings = new Settings();
            if (settings.Load(profile))
            {
                Settings = settings;
            }

            Title = $"Settings - {Settings.Name}";

            DataContext = Settings;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataContext)));
        }
    }
}
