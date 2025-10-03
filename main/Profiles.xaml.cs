using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace NBackTask;

public partial class Profiles : Window
{
    public ObservableCollection<string> Items { get; } = [];
    public string? SelectedProfile => lsvItems.SelectedItem as string;

    public Profiles()
    {
        InitializeComponent();

        var filenames = Directory.GetFiles(Settings.SettingsFolder, Settings.GetProfileFileName("*", validate: false));
        foreach (var filename in filenames)
            Items.Add(Settings.GetProfileNameFromFileName(filename));
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Items_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Delete && lsvItems.SelectedItem is string name)
        {
            if (MessageBox.Show("The profile will be deleted. Continue?", Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Items.RemoveAt(lsvItems.SelectedIndex);
                try
                {
                    File.Delete(Path.Combine(Settings.SettingsFolder, Settings.GetProfileFileName(name)));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
