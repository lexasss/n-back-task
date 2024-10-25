using System.IO;
using System.Windows.Forms;

namespace CttTest;

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
            var dialog = new Microsoft.Win32.OpenFolderDialog()
            {
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                _settings.LogFolder = dialog.FolderName;
            }
            else
            {
                return null;
            }
        }

        var filename = Path.Join(_settings.LogFolder, $"ctt-test-{DateTime.Now:u}.txt".ToPath());

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
            MessageBox.Show($"Cannot save data into '{filename}:\n{ex.Message}'", "CTT test", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return filename;
    }

    // Internal

    protected Logger() { }

    static Logger? _instance = null;

    readonly List<string> _records = [];
    readonly Settings _settings = Settings.Instance;

    long _startTime = DateTime.Now.Ticks;
}
