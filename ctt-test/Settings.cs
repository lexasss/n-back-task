using System.Windows.Media;

namespace CttTest;

internal class Settings
{
    public static SolidColorBrush ScreenColor { get; set; }
    public static SolidColorBrush ActiveScreenColor { get; set; }
    public static SolidColorBrush StimulusColor { get; set; }
    public static SolidColorBrush StimulusFontColor { get; set; }
    public static SolidColorBrush ActiveStimulusColor { get; set; }
    public static SolidColorBrush ActiveStimulusFontColor { get; set; }

    public static int StimulusBorderThickness { get; set; }
    public static double StimulusGap { get; set; }

    public static int BlankScreenDuration { get; set; } // ms
    public static int StimulusDuration { get; set; }    // ms
    public static int InfoDuration { get; set; }        // ms

    public static int TestCount { get; set; }

    public static string LogFolder { get; set; }

    static Settings()
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

        TestCount = settings.TestCount;
        LogFolder = settings.LogFolder;

        System.Windows.Application.Current.Exit += (s, e) =>
        {
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

            settings.TestCount = TestCount;
            settings.LogFolder = LogFolder;

            settings.Save();
        };
    }
}
