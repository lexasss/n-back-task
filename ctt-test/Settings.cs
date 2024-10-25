using System.ComponentModel;
using System.Windows.Media;

namespace CttTest;

internal enum InputMode
{
    Mouse,
    Touch
}

internal class Settings : INotifyPropertyChanged
{
    public static Settings Instance => _instance ??= new();

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
    public int InfoDuration { get; set; }        // ms
    public bool ActivationInterruptsTrial { get; set; }
    public bool AllowMultipleActivations { get; set; }

    public int TrialCount { get; set; }

    public InputMode InputMode { get; set; }

    public string LogFolder
    {
        get => _logFolder;
        set
        {
            _logFolder = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogFolder)));
        }
    }

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
        settings.InfoDuration = InfoDuration;
        settings.ActivationInterruptsTrial = ActivationInterruptsTrial;
        settings.AllowMultipleActivations = AllowMultipleActivations;

        settings.InputMode = (int)InputMode;
        settings.TestCount = TrialCount;
        settings.LogFolder = LogFolder;

        settings.Save();
    }

    // Internal

    static Settings? _instance = null;

    string _logFolder = "";

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
        InfoDuration = settings.InfoDuration;
        ActivationInterruptsTrial = settings.ActivationInterruptsTrial;
        AllowMultipleActivations = settings.AllowMultipleActivations;

        InputMode = (InputMode)settings.InputMode;
        TrialCount = settings.TestCount;
        LogFolder = settings.LogFolder;
    }
}
