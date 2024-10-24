using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CttTest;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Background = Settings.ScreenColor;

        grdSetup.Background = Settings.ActiveScreenColor;
        grdSetup.Visibility = Visibility.Hidden;

        _procedure.NextTask += Procedure_NextTask;
        _procedure.StimuliShown += Procedure_StimuliShown;
        _procedure.StimuliHidden += Procedure_StimuliHidden;
        _procedure.Finished += Procedure_Finished;

        LoadSetup(0);
    }

    // Internal

    readonly Procedure _procedure = new();
    readonly Random _random = new();

    readonly List<UIElement> _stimuliElements = [];

    CancellationTokenSource _cts = new();

    int _setupIndex = -1;

    private void DisplayInfo(string info, int delay = 0)
    {
        _cts.Cancel();

        if (delay > 0)
        {
            Task.Run(async () =>
            {
                try
                {
                    _cts = new();
                    await Task.Delay(delay, _cts.Token);
                    Dispatcher.Invoke(() => lblInfo.Content = info);
                }
                finally { }
            });
        }
        else
        {
            lblInfo.Content = info;
        }
    }

    private Stimulus[] ShuffleStimuli(Setup setup)
    {
        grdSetup.Children.Clear();

        var stimuliElements = _stimuliElements.ToArray();
        _random.Shuffle(stimuliElements);

        for (int i = 0; i < stimuliElements.Length; i++)
        {
            var el = stimuliElements[i];

            var (row, column) = setup.GetStimulusLocation(i);
            Grid.SetRow(el, row);
            Grid.SetColumn(el, column);

            grdSetup.Children.Add(el);
        }

        return stimuliElements
            .Select(el => ((el as Border)?.Child as Label)?.Tag as Stimulus)
            .Where(item => item != null)
            .ToArray()!;
    }

    private void LoadSetup(int index)
    {
        if (index < 0 || index >= _procedure.Setups.Length || index == _setupIndex)
            return;

        _setupIndex = index;

        var setup = _procedure.Setups[index];

        DisplayInfo($"Setup '{setup.Name}'");
        DisplayInfo("Press ENTER to start", 3000);

        grdSetup.Children.Clear();
        _stimuliElements.Clear();

        grdSetup.RowDefinitions.Clear();
        for (int i = 0; i < setup.RowCount; i++)
            grdSetup.RowDefinitions.Add(new RowDefinition());

        grdSetup.ColumnDefinitions.Clear();
        for (int i = 0; i < setup.ColumnCount; i++)
            grdSetup.ColumnDefinitions.Add(new ColumnDefinition());

        grdSetup.HorizontalAlignment = setup.Alignment;
        grdSetup.VerticalAlignment = setup.Alignment == HorizontalAlignment.Stretch ? VerticalAlignment.Stretch : VerticalAlignment.Center;

        for (int i = 0; i < setup.Stimuli.Length; i++)
        {
            var stimulus = setup.Stimuli[i];

            var border = new Border()
            {
                BorderBrush = Settings.StimulusFontColor,
                BorderThickness = new Thickness(Settings.StimulusBorderThickness),
                Margin = new Thickness(Settings.StimulusGap / 2)
            };

            var label = new Label()
            {
                Content = stimulus.Text,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 128,
                FontFamily = new FontFamily("Arial"),
                FontWeight = FontWeight.FromOpenTypeWeight(700),
                Tag = stimulus
            };

            if (setup.Alignment != HorizontalAlignment.Stretch)
            {
                label.Width = stimulus.Size;
                label.Height = stimulus.Size;
            }

            label.Background = Settings.StimulusColor;
            label.Foreground = Settings.StimulusFontColor;

            label.TouchDown += Stimulus_TouchDown;
            label.TouchUp += Stimulus_TouchUp;
            label.MouseDown += Stimulus_MouseDown;
            label.MouseUp += Stimulus_MouseUp;

            border.Child = label;

            var (row, column) = setup.GetStimulusLocation(i);
            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);

            grdSetup.Children.Add(border);
            _stimuliElements.Add(border);
        }
    }

    private void Procedure_NextTask(object? sender, Setup setup)
    {
        Dispatcher.Invoke(() =>
        {
            DisplayInfo("+");
            if (setup.StimuliOrder == StimuliOrder.Randomized)
            {
                var stimuliOrdered = ShuffleStimuli(setup);
                _procedure.LogStimuliOrder(stimuliOrdered);
            }
        });
    }

    private void Procedure_StimuliShown(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            grdSetup.Visibility = Visibility.Visible;
        });
    }

    private void Procedure_StimuliHidden(object? sender, bool isCorrect)
    {
        Dispatcher.Invoke(() =>
        {
            DisplayInfo(isCorrect ? "Success" : "Failed");
            grdSetup.Visibility = Visibility.Hidden;

            foreach (Border el in _stimuliElements)
            {
                var label = el.Child as Label;
                if (label != null)
                {
                    label.Background = Settings.StimulusColor;
                    label.Foreground = Settings.StimulusFontColor;
                }
            }
        });
    }

    private void Procedure_Finished(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            DisplayInfo("Finished!");
            DisplayInfo("Press ENTER to start", 2000);
        });
    }

    // UI

    private void Stimulus_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Label lbl)
            return;

        Stimulus? stimulus = lbl.Tag as Stimulus;

        if (_procedure.CanActivateStimulus(stimulus))
        {
            lbl.Background = Settings.ActiveStimulusColor;
            lbl.Foreground = Settings.ActiveStimulusFontColor;
        }

        lbl.CaptureMouse();
    }

    private void Stimulus_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Label lbl)
            return;

        lbl.Background = Settings.StimulusColor;
        lbl.Foreground = Settings.StimulusFontColor;

        if (lbl.IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        _procedure.DeactivateStimulus();
    }

    private void Stimulus_TouchDown(object? sender, TouchEventArgs e)
    {
        if (sender is not Label lbl)
            return;

        Stimulus? stimulus = lbl.Tag as Stimulus;

        if (_procedure.CanActivateStimulus(stimulus))
        {
            lbl.Background = Settings.ActiveStimulusColor;
            lbl.Foreground = Settings.ActiveStimulusFontColor;
        }

        e.TouchDevice.Capture(lbl);
    }

    private void Stimulus_TouchUp(object? sender, TouchEventArgs e)
    {
        if (sender is not Label lbl)
            return;

        lbl.Background = Settings.StimulusColor;
        lbl.Foreground = Settings.StimulusFontColor;

        if (e.TouchDevice.Captured == lbl)
        {
            lbl.ReleaseTouchCapture(e.TouchDevice);
        }

        _procedure.DeactivateStimulus();
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _procedure.Run(_setupIndex);
        }
        else if (e.Key == Key.Escape)
        {
            _procedure.Stop();

            grdSetup.Visibility = Visibility.Hidden;

            DisplayInfo("Interrupted");
            DisplayInfo("Press ENTER to start", 2000);
        }
        else if (e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            if (!_procedure.IsRunning)
            {
                int setupIndex = e.Key - Key.D1;
                LoadSetup(setupIndex);
            }
        }
        else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9)
        {
            if (!_procedure.IsRunning)
            {
                int setupIndex = e.Key - Key.NumPad1;
                LoadSetup(setupIndex);
            }
        }
    }
}