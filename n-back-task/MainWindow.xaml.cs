using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NBackTask;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Background = _settings.ScreenColor;
        grdSetup.Background = _settings.ActiveScreenColor;

        _settings.Updated += (s, e) =>
        {
            Background = _settings.ScreenColor;
            grdSetup.Background = _settings.ActiveScreenColor;

            foreach (Border el in _stimuliElements)
            {
                el.BorderBrush = _settings.StimulusFontColor;
                el.BorderThickness = new Thickness(_settings.StimulusBorderThickness);
                el.Margin = new Thickness(_settings.StimulusGap / 2);
                
                if (el.Child is Label label)
                {
                    label.Background = _settings.StimulusColor;
                    label.Foreground = _settings.StimulusFontColor;
                }
            }
        };

        grdSetup.Visibility = Visibility.Hidden;

        _procedure.NextTrial += Procedure_NextTrial;
        _procedure.StimuliShown += Procedure_StimuliShown;
        _procedure.StimuliHidden += Procedure_StimuliHidden;
        _procedure.Finished += Procedure_Finished;

        LoadSetup(0);
    }

    // Internal

    readonly Procedure _procedure = new();
    readonly Random _random = new();
    readonly Settings _settings = Settings.Instance;

    readonly List<UIElement> _stimuliElements = [];

    CancellationTokenSource _cts = new();

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
        if (index < 0 || index >= _procedure.Setups.Length || index == _settings.SetupIndex)
            return;

        _settings.SetupIndex = index;

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
                BorderBrush = _settings.StimulusFontColor,
                BorderThickness = new Thickness(_settings.StimulusBorderThickness),
                Margin = new Thickness(_settings.StimulusGap / 2)
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

            label.Background = _settings.StimulusColor;
            label.Foreground = _settings.StimulusFontColor;

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

    private void Procedure_NextTrial(object? sender, Setup setup)
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

    private void Procedure_StimuliHidden(object? sender, bool? isCorrect)
    {
        Dispatcher.Invoke(() =>
        {
            if (isCorrect != null && _settings.InfoDuration > 0)
            {
                DisplayInfo(isCorrect == true ? "Success" : "Failed");
            }
            grdSetup.Visibility = Visibility.Hidden;

            foreach (Border el in _stimuliElements)
            {
                var label = el.Child as Label;
                if (label != null)
                {
                    label.Background = _settings.StimulusColor;
                    label.Foreground = _settings.StimulusFontColor;
                }
            }
        });
    }

    private void Procedure_Finished(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            grdSetup.Visibility = Visibility.Hidden;

            DisplayInfo("Finished!");
            DisplayInfo("Press ENTER to start", 2000);
        });
    }

    // UI

    private void Stimulus_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.InputMode != InputMode.Mouse)
            return;

        if (sender is not Label lbl)
            return;

        Stimulus? stimulus = lbl.Tag as Stimulus;

        if (_procedure.ActivateStimulus(stimulus))
        {
            lbl.Background = _settings.ActiveStimulusColor;
            lbl.Foreground = _settings.ActiveStimulusFontColor;
        }
    }

    private void Stimulus_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_settings.InputMode != InputMode.Mouse)
            return;

        if (sender is not Label lbl)
            return;

        lbl.Background = _settings.StimulusColor;
        lbl.Foreground = _settings.StimulusFontColor;

        _procedure.DeactivateStimulus();
    }

    private void Stimulus_TouchDown(object? sender, TouchEventArgs e)
    {
        if (_settings.InputMode != InputMode.Touch)
            return;

        if (sender is not Label lbl)
            return;

        Stimulus? stimulus = lbl.Tag as Stimulus;

        if (_procedure.ActivateStimulus(stimulus))
        {
            lbl.Background = _settings.ActiveStimulusColor;
            lbl.Foreground = _settings.ActiveStimulusFontColor;
        }
    }

    private void Stimulus_TouchUp(object? sender, TouchEventArgs e)
    {
        if (_settings.InputMode != InputMode.Touch)
            return;

        if (sender is not Label lbl)
            return;

        lbl.Background = _settings.StimulusColor;
        lbl.Foreground = _settings.StimulusFontColor;

        _procedure.DeactivateStimulus();
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (!_procedure.IsRunning)
            {
                _procedure.Run(_settings.SetupIndex);
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (_procedure.IsRunning)
            {
                _procedure.Stop();

                grdSetup.Visibility = Visibility.Hidden;

                DisplayInfo("Interrupted");
                DisplayInfo("Press ENTER to start", 2000);
            }
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
        else if (e.Key == Key.F2)
        {
            if (!_procedure.IsRunning)
            {
                _settings.ShowDialog();
            }
        }
        else if (e.Key == Key.F3)
        {
            if (!_procedure.IsRunning)
            {
                _procedure.ShowSetupEditor();
            }
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _settings.Save();
    }
}