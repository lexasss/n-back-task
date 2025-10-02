using System.ComponentModel;
using System.Windows.Media;

namespace NBackTask;

internal enum InputMode
{
    Mouse,
    Touch
}

internal enum SessionType
{
    Count,
    Duration
}

internal enum TaskType
{
    NBack,
    OneBack
}

internal class Settings : INotifyPropertyChanged
{
    public static Settings Instance => _instance ??= new();

    // Inter-session

    public SolidColorBrush ScreenColor { get; set; }
    public SolidColorBrush ActiveScreenColor { get; set; }
    public SolidColorBrush StimulusColor { get; set; }
    public SolidColorBrush StimulusFontColor { get; set; }
    public SolidColorBrush ActiveStimulusColor { get; set; }
    public SolidColorBrush ActiveStimulusFontColor { get; set; }

    public int StimulusBorderThickness { get; set; }
    public double StimulusGap { get; set; }

    public int BlankScreenDuration { get; set; } // ms
    public int StimulusDuration { get; set; }    // ms
    public bool IsTrialInfinite { get; set; }
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
    public string OneBackStimulus { get; set; } // 0..9
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
            modifiedSettings.Save();

            Load();
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Save()
    {
        var settings = Properties.Settings.Default;

        settings.ScreenColor = ScreenColor.Serialize();
        settings.ActiveScreenColor = ActiveScreenColor.Serialize();
        settings.StimulusColor = StimulusColor.Serialize();
        settings.StimulusFontColor = StimulusFontColor.Serialize();
        settings.ActiveStimulusColor = ActiveStimulusColor.Serialize();
        settings.ActiveStimulusFontColor = ActiveStimulusFontColor.Serialize();

        settings.StimulusBorderThickness = StimulusBorderThickness;
        settings.StimulusGap = StimulusGap;

        settings.BlankScreenDuration = BlankScreenDuration;
        settings.StimulusDuration = StimulusDuration;
        settings.IsTrialInfinite = IsTrialInfinite;
        settings.InfoDuration = InfoDuration;
        settings.ActivationInterruptsTrial = ActivationInterruptsTrial;
        settings.AllowMultipleActivations = AllowMultipleActivations;
        settings.StimulusUnstretchedSize = StimulusUnstretchedSize;
        settings.PlaySoundOnActivation = PlaySoundOnActivation;

        settings.InputMode = (int)InputMode;
        settings.SessionType = (int)SessionType;
        settings.TestCount = TrialCount;
        settings.SessionDuration = SessionDuration;
        settings.OneBackStimulus = OneBackStimulus;
        settings.TaskType = (int)TaskType;
        settings.LogFolder = LogFolder;

        settings.PlayBackgroundNoise = PlayBackgroundNoise;

        settings.Save();
    }

    // Internal

    static Settings? _instance = null;

#pragma warning disable CS8618
    private Settings()
    {
        Load();
    }
#pragma warning restore CS8618

    private void Load()
    {
        var settings = Properties.Settings.Default;

        ScreenColor = SolidColorBrushExt.Deserialize(settings.ScreenColor);
        ActiveScreenColor = SolidColorBrushExt.Deserialize(settings.ActiveScreenColor);
        StimulusColor = SolidColorBrushExt.Deserialize(settings.StimulusColor);
        StimulusFontColor = SolidColorBrushExt.Deserialize(settings.StimulusFontColor);
        ActiveStimulusColor = SolidColorBrushExt.Deserialize(settings.ActiveStimulusColor);
        ActiveStimulusFontColor = SolidColorBrushExt.Deserialize(settings.ActiveStimulusFontColor);

        StimulusBorderThickness = settings.StimulusBorderThickness;
        StimulusGap = settings.StimulusGap;

        BlankScreenDuration = settings.BlankScreenDuration;
        StimulusDuration = settings.StimulusDuration;
        IsTrialInfinite = settings.IsTrialInfinite;
        InfoDuration = settings.InfoDuration;
        ActivationInterruptsTrial = settings.ActivationInterruptsTrial;
        AllowMultipleActivations = settings.AllowMultipleActivations;
        StimulusUnstretchedSize = settings.StimulusUnstretchedSize;
        PlaySoundOnActivation = settings.PlaySoundOnActivation;

        InputMode = (InputMode)settings.InputMode;
        SessionType = (SessionType)settings.SessionType;
        TrialCount = settings.TestCount;
        SessionDuration = settings.SessionDuration;
        OneBackStimulus = settings.OneBackStimulus;
        TaskType = (TaskType)settings.TaskType;
        LogFolder = settings.LogFolder;

        PlayBackgroundNoise = settings.PlayBackgroundNoise;
    }
}
