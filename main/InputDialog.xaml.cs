using System.Windows;

namespace NBackTask;

public partial class InputDialog : Window
{
    public string Prompt { get; set; } = "Please enter a text:";
    public string Text { get; set; } = "setup";

    public static string? ShowDialog(string prompt, string? defaultValue = null)
    {
        var dialog = new InputDialog();
        dialog.Prompt = prompt;
        if (!string.IsNullOrEmpty(defaultValue))
        {
            dialog.Text = defaultValue;
        }

        dialog.DataContext = dialog;
        if (dialog.ShowDialog() == true)
        {
            return dialog.Text;
        }

        return null;
    }

    // Internal

    private InputDialog()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
