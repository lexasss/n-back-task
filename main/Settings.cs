using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace NBackTask;

public enum InputMode
{
    Mouse,
    Touch
}

public enum SessionType
{
    Count,
    Duration
}

public enum TaskType
{
    NBack,
    ZeroBack
}

public enum TrialDurationType
{
    Timed,
    Infinite
}

public class Settings : INotifyPropertyChanged
{
    public static Settings Instance { get => field ??= new(); } = null;

    [JsonIgnore]
    public string? Name { get; set; } = null;

    // Inter-session

    [JsonIgnore]
    public SolidColorBrush ScreenColor { get; set; }
    [JsonIgnore]
    public SolidColorBrush ActiveScreenColor { get; set; }
    [JsonIgnore]
    public SolidColorBrush StimulusColor { get; set; }
    [JsonIgnore]
    public SolidColorBrush StimulusFontColor { get; set; }
    [JsonIgnore]
    public SolidColorBrush ActiveStimulusColor { get; set; }
    [JsonIgnore]
    public SolidColorBrush ActiveStimulusFontColor { get; set; }

    public string ScreenColorAsText
    {
        get => ScreenColor.Serialize();
        set => ScreenColor = SolidColorBrushExt.Deserialize(value);
    }
    public string ActiveScreenColorAsText {
        get => ActiveScreenColor.Serialize();
        set => ActiveScreenColor = SolidColorBrushExt.Deserialize(value);
    }
    public string StimulusColorAsText
    {
        get => StimulusColor.Serialize();
        set => StimulusColor = SolidColorBrushExt.Deserialize(value);
    }
    public string StimulusFontColorAsText {
        get => StimulusFontColor.Serialize();
        set => StimulusFontColor = SolidColorBrushExt.Deserialize(value);
    }
    public string ActiveStimulusColorAsText {
        get => ActiveStimulusColor.Serialize();
        set => ActiveStimulusColor = SolidColorBrushExt.Deserialize(value);
    }
    public string ActiveStimulusFontColorAsText {
        get => ActiveStimulusFontColor.Serialize();
        set => ActiveStimulusFontColor = SolidColorBrushExt.Deserialize(value);
    }

    public int StimulusBorderThickness { get; set; }
    public double StimulusGap { get; set; }

    public int BlankScreenDuration { get; set; } // ms
    public int StimulusDuration { get; set; }    // ms
    public TrialDurationType TrialDurationType
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrialDurationType)));
        }
    }
    public int InfoDuration { get; set; }        // ms
    public bool ActivationInterruptsTrial { get; set; }
    public bool AllowMultipleActivations { get; set; }
    public int StimulusUnstretchedSize { get; set; }
    public bool PlaySoundOnActivation { get; set; }

    public InputMode InputMode { get; set; }
    public SessionType SessionType
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionType)));
        }
    } = SessionType.Count;
    public int TrialCount { get; set; }
    public int SessionDuration { get; set; }  // seconds
    public string ZeroBackStimulus { get; set; } // 0..9
    public TaskType TaskType
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskType)));
        }
    } = TaskType.NBack;

    public string LogFolder
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogFolder)));
        }
    } = "";

    public bool PlayBackgroundNoise
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayBackgroundNoise)));
        }
    } = false;

    // Session-only

    public int SetupIndex { get; set; } = -1;


    public event EventHandler? Updated;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowDialog()
    {
        var modifiedSettings = new Settings();
        var dialog = new SettingsDialog(modifiedSettings);
        if (dialog.ShowDialog() ?? false)
        {
            modifiedSettings = dialog.Settings;
            modifiedSettings.Save();

            if (!string.IsNullOrEmpty(modifiedSettings.Name))
            {
                if (Load(modifiedSettings.Name))
                {
                    _loadingName = modifiedSettings.Name;
                }
            }
            else
            {
                Load();
            }

            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Save(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        try
        {
            var filename = GetProfileFilePath(name);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filename, json);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save settings '{name}': {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void Save()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            Save(Name);
            return;
        }

        var settings = Properties.Settings.Default;

        settings.ScreenColor = ScreenColorAsText;
        settings.ActiveScreenColor = ActiveScreenColorAsText;
        settings.StimulusColor = StimulusColorAsText;
        settings.StimulusFontColor = StimulusFontColorAsText;
        settings.ActiveStimulusColor = ActiveStimulusColorAsText;
        settings.ActiveStimulusFontColor = ActiveStimulusFontColorAsText;

        settings.StimulusBorderThickness = StimulusBorderThickness;
        settings.StimulusGap = StimulusGap;

        settings.BlankScreenDuration = BlankScreenDuration;
        settings.StimulusDuration = StimulusDuration;
        settings.TrialDurationType = (int)TrialDurationType;
        settings.InfoDuration = InfoDuration;
        settings.ActivationInterruptsTrial = ActivationInterruptsTrial;
        settings.AllowMultipleActivations = AllowMultipleActivations;
        settings.StimulusUnstretchedSize = StimulusUnstretchedSize;
        settings.PlaySoundOnActivation = PlaySoundOnActivation;

        settings.InputMode = (int)InputMode;
        settings.SessionType = (int)SessionType;
        settings.TestCount = TrialCount;
        settings.SessionDuration = SessionDuration;
        settings.ZeroBackStimulus = ZeroBackStimulus;
        settings.TaskType = (int)TaskType;
        settings.LogFolder = LogFolder;

        settings.PlayBackgroundNoise = PlayBackgroundNoise;

        settings.Save();
    }

    public bool Load(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        try
        {
            var filename = GetProfileFilePath(name);
            if (System.IO.File.Exists(filename))
            {
                var json = System.IO.File.ReadAllText(filename);

                _isSerializing = true;
                var result = JsonSerializer.Deserialize<Settings>(json);
                _isSerializing = false;

                if (result != null)
                {
                    result.SetupIndex = SetupIndex;

                    // copy all properties
                    foreach (var prop in typeof(Settings).GetProperties())
                    {
                        // skip ignored properties
                        object[] attrs = prop.GetCustomAttributes(true);
                        if (attrs.Any(attr => attr is JsonIgnoreAttribute))
                            continue;

                        if (prop.CanRead && prop.CanWrite)
                        {
                            var value = prop.GetValue(result);
                            prop.SetValue(this, value);
                        }
                    }
                    Name = name;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save settings '{name}': {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        return false;
    }

    public static string SettingsFolder =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tampere_University");

    public static string GetProfileFileName(string name, bool validate = true)
    {
        if (validate)
            name = name.ToPath();
        return $"nbacktask-{name}.settings";
    }

    public static string GetProfileNameFromFileName(string filename)
    {
        var fn = System.IO.Path.GetFileNameWithoutExtension(filename);
        var p = fn.Split('-');
        return string.Join('-', p.Skip(1)); // skip nbacktask-
    }

    // Internal

    static string? _loadingName = null;
    static bool _isSerializing = false;

#pragma warning disable CS8618
    // To be used only by serializer
    public Settings()
    {
        if (!_isSerializing)
            Load();
    }
#pragma warning restore CS8618

    static Settings()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
            _loadingName = args[1];
    }

    private static string GetProfileFilePath(string name)
    {
        return System.IO.Path.Combine(SettingsFolder, GetProfileFileName(name));
    }

    private void Load()
    {
        if (!string.IsNullOrEmpty(_loadingName) && Load(_loadingName))
            return;

        var settings = Properties.Settings.Default;

        ScreenColorAsText = settings.ScreenColor;
        ActiveScreenColorAsText = settings.ActiveScreenColor;
        StimulusColorAsText = settings.StimulusColor;
        StimulusFontColorAsText = settings.StimulusFontColor;
        ActiveStimulusColorAsText = settings.ActiveStimulusColor;
        ActiveStimulusFontColorAsText = settings.ActiveStimulusFontColor;

        StimulusBorderThickness = settings.StimulusBorderThickness;
        StimulusGap = settings.StimulusGap;

        BlankScreenDuration = settings.BlankScreenDuration;
        StimulusDuration = settings.StimulusDuration;
        TrialDurationType = (TrialDurationType)settings.TrialDurationType;
        InfoDuration = settings.InfoDuration;
        ActivationInterruptsTrial = settings.ActivationInterruptsTrial;
        AllowMultipleActivations = settings.AllowMultipleActivations;
        StimulusUnstretchedSize = settings.StimulusUnstretchedSize;
        PlaySoundOnActivation = settings.PlaySoundOnActivation;

        InputMode = (InputMode)settings.InputMode;
        SessionType = (SessionType)settings.SessionType;
        TrialCount = settings.TestCount;
        SessionDuration = settings.SessionDuration;
        ZeroBackStimulus = settings.ZeroBackStimulus;
        TaskType = (TaskType)settings.TaskType;
        LogFolder = settings.LogFolder;

        PlayBackgroundNoise = settings.PlayBackgroundNoise;
    }
}
