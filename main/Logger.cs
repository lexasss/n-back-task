using System.IO;
using System.Windows;

namespace NBackTask;

internal enum LogSource
{
    Experiment,
    Stimuli,
    Stimulus
}

internal enum LogAction
{
    Start,
    Stop,
    Result,
    Target,
    Displayed,
    Activated,
    Hidden,
    Ordered
}

internal record class LogRecord(LogSource Source, LogAction Action, params object[] Args)
{
    public long Timestamp = DateTime.Now.Ticks;
    public string AsString() => string.Join('\t', [Timestamp, Source, Action, ..Args]);
}

internal class Logger
{
    public static Logger Instance => _instance ??= new();

    public void Reset()
    {
        lock (_records)
        {
            _records.Clear();
        }
    }

    public void Add(LogSource source, LogAction action, params object[] args)
    {
        lock (_records)
        {
            _records.Add(new LogRecord(source, action, args));
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
                    writer.WriteLine(record.AsString());
                }

                Statistics.CreateAndWriteToFile(_records, writer);

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

    public static string ReadSummaryFromFile(string filename)
    {
        using var reader = new StreamReader(filename);
        string? line;
        bool isSummarySection = false;
        List<string> summary = [];
        while ((line = reader.ReadLine()) != null)
        {
            if (isSummarySection)
            {
                summary.Add(line);
            }
            else if (line.StartsWith("#"))
            {
                isSummarySection = true;
            }
        }
        return string.Join(Environment.NewLine, summary);
    }

    // Internal

    protected Logger() { }

    static Logger? _instance = null;

    readonly List<LogRecord> _records = [];
    readonly Settings _settings = Settings.Instance;
}

file static class Statistics
{
    public static void CreateAndWriteToFile(IEnumerable<LogRecord> records, StreamWriter writer)
    {
        List<StatRecord> stat = new();

        string? target = null;
        string? response = null;
        int clicks = 0;
        long targetDisplayTimestamp = 0;
        long responseTimestamp = 0;

        foreach (var record in records)
        {
            if (record.Source == LogSource.Stimuli && record.Action == LogAction.Target)
            {
                target = (string)record.Args[0];
                response = null;
                clicks = 0;
                responseTimestamp = 0;
            }
            else if (record.Source == LogSource.Stimuli && record.Action == LogAction.Displayed)
            {
                targetDisplayTimestamp = record.Timestamp;
            }
            else if (record.Source == LogSource.Stimulus && record.Action == LogAction.Activated)
            {
                if (target != null)
                {
                    clicks += 1;

                    if (response == null)
                    {
                        response = (string)record.Args[0];
                        responseTimestamp = record.Timestamp;
                    }
                }
            }
            else if (record.Source == LogSource.Stimuli && record.Action == LogAction.Hidden)
            {
                var isMissingCorrectActivation = target != response;

                stat.Add(new StatRecord(target ?? "?",
                    response ?? "",
                    (isMissingCorrectActivation, clicks) switch
                    {
                        (true, 0) => Result.MISS,
                        (true, > 0) => Result.FAIL,
                        _ => Result.OK,
                    },
                    isMissingCorrectActivation ? "" : ((responseTimestamp - targetDisplayTimestamp) / 10000).ToString(),
                    clicks
                ));

                target = null;
            }
        }

        writer.WriteLine("#");
        writer.WriteLine(string.Join('\t', ["Target", "Response", "Result", "Interval", "Count"]));
        foreach (var record in stat)
        {
            writer.WriteLine(record.AsString());
        }
    }

    // Internal

    enum Result
    {
        OK,
        FAIL,
        MISS
    }
    private record class StatRecord(string Target, string Response, Result Result, string Interval, int Count)
    {
        public string AsString() => string.Join('\t', [Target, Response, Result, Interval, Count]);
    }
}
