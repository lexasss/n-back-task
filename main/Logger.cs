using System.IO;
using System.Windows;

namespace NBackTask;

internal class Logger
{
    public static Logger Instance => _instance ??= new();

    public void Reset()
    {
        _startTime = DateTime.Now.Ticks;
        lock (_records)
        {
            _records.Clear();
        }
    }

    public void Add(params object[] items)
    {
        var timestamp = (DateTime.Now.Ticks - _startTime) / 10000;
        var record = string.Join('\t', [timestamp, ..items]);

        lock (_records)
        {
            _records.Add(record);
        }
    }

    public string? Save()
    {
        if (string.IsNullOrEmpty(_settings.LogFolder))
        {
            var folderName = SelectLogFolder(_settings.LogFolder);
            if (folderName != null)
                _settings.LogFolder = folderName;
            else
                return null;
        }

        var filename = Path.Join(_settings.LogFolder, $"n-back-task-{DateTime.Now:u}.txt".ToPath());

        try
        {
            using var writer = new StreamWriter(filename);

            lock (_records)
            {
                foreach (var record in _records)
                {
                    writer.WriteLine(record);
                }

                _records.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            filename = null;
            MessageBox.Show($"Cannot save data into '{filename}':\n{ex.Message}", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return filename;
    }

    public static string? SelectLogFolder(string? folderName = null)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog()
        {
            Title = $"Select a folder to store {App.Name} log files",
            DefaultDirectory = !string.IsNullOrEmpty(folderName) ?
                folderName :
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FolderName;
        }

        return null;
    }

    // Internal

    protected Logger() { }

    static Logger? _instance = null;

    readonly List<string> _records = [];
    readonly Settings _settings = Settings.Instance;

    long _startTime = DateTime.Now.Ticks;
}
