using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NBackTask;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Background = _settings.ScreenColor;
        grdSetup.Background = _settings.ActiveScreenColor;
        grdSetup.Visibility = Visibility.Hidden;
        wplButtons.Visibility = Visibility.Hidden;

        _fontSizeController = new FontSizeController(this);

        _settings.Updated += (s, e) => UpdateStimuli();

        _procedure.Started += Procedure_Started;
        _procedure.NextTrial += Procedure_NextTrial;
        _procedure.StimuliShown += Procedure_StimuliShown;
        _procedure.StimuliHidden += Procedure_StimuliHidden;
        _procedure.Stopped += Procedure_Finished;
        _procedure.ConnectionStatusChanged += Procedure_ConnectionStatusChanged;

        Procedure_ConnectionStatusChanged(null, false);

        LoadSetup(0);
        UpdateSetupButtonState();
    }

    // Internal

    readonly Procedure _procedure = new();
    readonly Random _random = new();
    readonly Settings _settings = Settings.Instance;

    readonly ImageSource _tcpOnImage = new BitmapImage(new Uri("pack://application:,,,/Assets/images/tcp-on.png"));
    readonly ImageSource _tcpOffImage = new BitmapImage(new Uri("pack://application:,,,/Assets/images/tcp-off.png"));

    readonly List<UIElement> _stimuliElements = [];
    readonly FontSizeController _fontSizeController;

    CancellationTokenSource _cts = new();

    bool _isDebugMode = false;
    bool _windowWasMaximized = false;

    private void SetFullScreen(bool isFullScreen)
    {
        if (isFullScreen)
        {
            _windowWasMaximized = WindowState == WindowState.Maximized;
        }

        WindowState = WindowState.Normal;
        WindowStyle = isFullScreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;

        WindowState = _windowWasMaximized || isFullScreen ? WindowState.Maximized : WindowState.Normal;

        tblInstructions.Visibility = isFullScreen ? Visibility.Hidden : Visibility.Visible;
        imgTcpClient.Visibility = isFullScreen ? Visibility.Hidden : Visibility.Visible;
    }

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

        Title = $"{App.Name}: {setup.Name}";

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

        _fontSizeController.SetStimulusSize(_settings.StimulusUnstretchedSize);
        _fontSizeController.RowsInLayout = setup.RowCount;

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
                FontFamily = new FontFamily("Arial"),
                FontWeight = FontWeight.FromOpenTypeWeight(700),
                Padding = new Thickness(0),
                Tag = stimulus
            };

            label.SetBinding(FontSizeProperty, _fontSizeController.Binding);

            if (setup.Alignment != HorizontalAlignment.Stretch)
            {
                border.Width = _settings.StimulusUnstretchedSize;
                border.Height = _settings.StimulusUnstretchedSize;
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

    private void UpdateStimuli()
    {
        Background = _settings.ScreenColor;
        grdSetup.Background = _settings.ActiveScreenColor;

        var setup = _procedure.Setups[_settings.SetupIndex];
        _fontSizeController.SetStimulusSize(_settings.StimulusUnstretchedSize);

        foreach (Border border in _stimuliElements)
        {
            border.BorderBrush = _settings.StimulusFontColor;
            border.BorderThickness = new Thickness(_settings.StimulusBorderThickness);
            border.Margin = new Thickness(_settings.StimulusGap / 2);

            if (border.Child is Label label)
            {
                label.Background = _settings.StimulusColor;
                label.Foreground = _settings.StimulusFontColor;
            }

            if (setup.Alignment != HorizontalAlignment.Stretch)
            {
                border.Width = _settings.StimulusUnstretchedSize;
                border.Height = _settings.StimulusUnstretchedSize;
            }
        }
    }

    private void UpdateSetupButtonState()
    {
        var setupStudy = FindResource("Setup") as Style;
        var setupButtons = wplButtons.Children.OfType<Button>().Where(el => el.Style == setupStudy);
        for (int i = 0; i < setupButtons.Count(); i++)
        {
            setupButtons.ElementAt(i).IsEnabled = i < _procedure.Setups.Length;
        }
    }

    // Event handlers

    private void Procedure_Started(object? sender, EventArgs e) => Dispatcher.Invoke(() =>
    {
        if (_isDebugMode)
            wplButtons.Visibility = Visibility.Hidden;

        SetFullScreen(true);
    });

    private void Procedure_NextTrial(object? sender, Setup setup) => Dispatcher.Invoke(() =>
    {
        DisplayInfo("+");
        if (setup.StimuliOrder == StimuliOrder.Randomized)
        {
            var stimuliOrdered = ShuffleStimuli(setup);
            _procedure.LogStimuliOrder(stimuliOrdered);
        }
    });

    private void Procedure_StimuliShown(object? sender, EventArgs e) => Dispatcher.Invoke(() =>
    {
        grdSetup.Visibility = Visibility.Visible;
    });

    private void Procedure_StimuliHidden(object? sender, bool? isCorrect) => Dispatcher.Invoke(() =>
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

    private void Procedure_Finished(object? sender, Procedure.StopReason stopReason) => Dispatcher.Invoke(() =>
    {
        grdSetup.Visibility = Visibility.Hidden;

        if (_isDebugMode)
            wplButtons.Visibility = Visibility.Visible;

        SetFullScreen(false);

        DisplayInfo(stopReason == Procedure.StopReason.Finished ? "Finished!" : "Interrupted");
        DisplayInfo("Press ENTER to start", 2000);
    });

    private void Procedure_ConnectionStatusChanged(object? sender, bool isConnected) => Dispatcher.Invoke(() =>
    {
        if (_procedure.IsServerReady)
        {
            imgTcpClient.Source = isConnected ? _tcpOnImage : _tcpOffImage;
        }
    });

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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _procedure.Run(_settings.SetupIndex);
        }
        else if (e.Key == Key.Escape)
        {
            if (_procedure.IsRunning)
            {
                _procedure.Stop();
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
                _isDebugMode = !_isDebugMode;
                wplButtons.Visibility = _isDebugMode ? Visibility.Visible : Visibility.Hidden;
            }
        }
        else if (e.Key == Key.F5)
        {
            if (!_procedure.IsRunning)
            {
                _settings.ShowDialog();
            }
        }
        else if (e.Key == Key.F6)
        {
            if (!_procedure.IsRunning)
            {
                if (_procedure.ShowSetupEditor() is int selectedIndex)
                {
                    LoadSetup(selectedIndex);
                    UpdateSetupButtonState();
                }
            }
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _settings.Save();
    }

    private void SetupButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_procedure.IsRunning)
        {
             if (int.TryParse((sender as Button)?.Content.ToString(), out int setupIndex))
                LoadSetup(setupIndex - 1);
        }
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_procedure.IsRunning)
        {
            _settings.ShowDialog();
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _procedure.Run(_settings.SetupIndex);
    }
}