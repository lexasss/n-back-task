using System.Media;
using System.Text.Json;
using System.Windows;

namespace NBackTask;

internal class Procedure
{
    public enum StopReason { Interrupted, Finished }

    public Setup[] Setups => _setups.ToArray();
    public Setup? CurrentSetup { get; private set; } = null;

    public bool IsRunning => _state != State.Inactive;
    public bool IsServerReady => _server.IsListening;

    public event EventHandler? Started;
    public event EventHandler<Setup>? NextTrial;
    public event EventHandler? StimuliShown;
    public event EventHandler<bool?>? StimuliHidden;
    public event EventHandler<StopReason>? Stopped;
    public event EventHandler<bool>? ConnectionStatusChanged;
    public event EventHandler<int>? SetupRequested;

    public Procedure()
    {
        _timer.AutoReset = false;
        _timer.Elapsed += Timer_Elapsed;

        _setups = LoadSetups();

        var audioFilenames = _setups.SelectMany(setup => setup.Stimuli.Select(stimulus => stimulus.AudioInstruction)).ToList();
        audioFilenames.Add(SOUND_CORRECT);
        audioFilenames.Add(SOUND_INCORRECT);

        _player.CheckSoundsExist(audioFilenames.ToArray());

        _server.ClientConnected += (s, e) => ConnectionStatusChanged?.Invoke(this, true);
        _server.ClientDisconnected += (s, e) => ConnectionStatusChanged?.Invoke(this, false);
        _server.Data += Server_Data;

        _server.Start();

        UpdateBackgroundNoiseState();

        _settings.Updated += (s, e) => UpdateBackgroundNoiseState();

        Application.Current.Exit += (s, e) =>
        {
            _backgroundSound.Stop();
            SaveSetups();
        };
    }

    public async void Run(int setupIndex)
    {
        if (_state != State.Inactive)
            return;

        if (setupIndex < 0 || setupIndex >= _setups.Count)
            return;

        CurrentSetup = _setups[setupIndex];

        _logger.Reset();
        _logger.Add(LogSource.Experiment, LogAction.Start, CurrentSetup.Name);

        _server.Send($"STR");

        _targetIndexes = PrepareTargets(CurrentSetup);

        _trialIndex = -1;

        Started?.Invoke(this, EventArgs.Empty);
        _player.PlayStartSound();

        await Task.Delay(500);

        Next();
    }

    public void Stop(StopReason reason = StopReason.Interrupted)
    {
        if (_state == State.Inactive)
            return;

        _timer.Stop();

        UpdateState(State.Inactive);

        Stopped?.Invoke(this, reason);
    }

    public bool ActivateStimulus(Stimulus? stimulus)
    {
        bool wasActivated = true;

        if (_settings.AllowMultipleActivations)
        {
            CurrentSetup?.ResetStimuli();
        }
        else
        {
            wasActivated = CurrentSetup?.GetActiveStimulus() is null;
        }

        if (wasActivated && stimulus != null)
        {
            stimulus.WasActivated = true;
            _logger.Add(LogSource.Stimulus, LogAction.Activated, stimulus.Text);
            _server.Send($"ACT {stimulus.Text}");

            if (_settings.PlaySoundOnActivation)
            {
                _activationSound.Play();
            }

            System.Diagnostics.Debug.WriteLine($"Activated: {stimulus.Text}");
        }

        return wasActivated;
    }

    public void DeactivateStimulus()
    {
        if (_settings.ActivationInterruptsTrial)
        {
            _timer.Stop();
            UpdateState(State.Info);
        }
    }

    public void LogStimuliOrder(Stimulus[] stimuli)
    {
        _logger.Add(LogSource.Stimuli, LogAction.Ordered, string.Join(' ', stimuli.Select(s => s.Text)));
    }

    public int? ShowSetupEditor()
    {
        var setups = _setups.Select(SetupData.From).ToList();
        var dialog = new SetupEditor(setups, _settings.SetupIndex);
        if (dialog.ShowDialog() == true)
        {
            _setups.Clear();
            foreach (var sd in setups)
            {
                _setups.Add(new Setup(sd));
            }

            return dialog.SelectedSetupIndex;
        }

        return null;
    }

    // Internal

    enum State
    {
        Inactive,
        BlankScreen,
        Stimuli,
        Info
    }

    const string SOUND_CORRECT = "success";
    const string SOUND_INCORRECT = "failed";

    readonly string NET_COMMAND_START = "start";
    readonly string NET_COMMAND_STOP = "stop";
    readonly string NET_COMMAND_SET_TASK = "set";
    readonly string NET_COMMAND_EXIT = "exit";

    readonly System.Timers.Timer _timer = new();
    readonly Player _player = new();
    readonly Logger _logger = Logger.Instance;
    readonly Settings _settings = Settings.Instance;
    readonly Random _random = new();
    readonly List<Setup> _setups = [];
    readonly Sound _backgroundSound = new("assets/sounds/noise.mp3", "background");
    readonly Sound _activationSound = new("assets/sounds/activation.mp3", "activation");

    readonly TcpServer _server = new();
    readonly StringComparison _stringComparison = StringComparison.OrdinalIgnoreCase;

    State _state = State.Inactive;
    int[] _targetIndexes = [];
    int _trialIndex = -1;

    private List<Setup> LoadSetups()
    {
        var setups = new List<Setup>();

        var settings = Properties.Settings.Default;

        if (!string.IsNullOrEmpty(settings.Setups) &&
            JsonSerializer.Deserialize<SetupData[]>(settings.Setups) is SetupData[] setupItems)
        {
            foreach (SetupData sd in setupItems)
            {
                setups.Add(new Setup(sd));
            }
        }

        if (setups.Count == 0)
        {
            setups.Add(new Setup("Very Easy", 1, 2, HorizontalAlignment.Stretch, StimuliOrder.Ordered));
            setups.Add(new Setup("Easy", 2, 2, HorizontalAlignment.Stretch, StimuliOrder.Ordered));
            setups.Add(new Setup("Moderate", 2, 5, HorizontalAlignment.Stretch, StimuliOrder.Ordered));
            setups.Add(new Setup("Hard", 2, 2, HorizontalAlignment.Stretch, StimuliOrder.Randomized));
            setups.Add(new Setup("Very Hard", 2, 5, HorizontalAlignment.Stretch, StimuliOrder.Randomized));
        }

        return setups;
    }

    private void SaveSetups()
    {
        var setups = _setups.Select(SetupData.From).ToArray();
        var setupString = JsonSerializer.Serialize(setups);

        Properties.Settings.Default.Setups = setupString;
        Properties.Settings.Default.Save();
    }

    private void Next()
    {
        if (CurrentSetup == null)
            return;

        CurrentSetup.ResetStimuli();

        if (++_trialIndex < _settings.TrialCount)
        {
            UpdateState(State.BlankScreen);
        }
        else
        {
            Stop(StopReason.Finished);

            var filename = _logger.Save();
            if (filename == null)
            {
                MessageBox.Show("Failed to save data!", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                //MessageBox.Show($"Data saved to '{filename}'", App.Name, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void UpdateState(State newState)
    {
        if (CurrentSetup == null)
            return;

        _state = newState;

        if (_state == State.BlankScreen)
        {
            _timer.Interval = Math.Max(1, _settings.BlankScreenDuration);
            _timer.Start();

            var stimulus = CurrentSetup.Stimuli[_targetIndexes[_trialIndex]];
            _logger.Add(LogSource.Stimuli, LogAction.Target, stimulus?.Text ?? "?");

            if (_settings.InfoDuration == 0 && _settings.BlankScreenDuration > 0)
            {
                _server.Send($"HID {stimulus?.Text}");
                StimuliHidden?.Invoke(this, null);
            }
            NextTrial?.Invoke(this, CurrentSetup);
        }
        else if (_state == State.Stimuli)
        {
            _timer.Interval = _settings.StimulusDuration;
            _timer.Start();

            StimuliShown?.Invoke(this, EventArgs.Empty);

            var stimulus = CurrentSetup.Stimuli[_targetIndexes[_trialIndex]];
            _logger.Add(LogSource.Stimuli, LogAction.Displayed, stimulus?.Text ?? "?");

            if (stimulus != null)
            {
                _server.Send($"SET {stimulus.Text}");

                var sound = stimulus.AudioInstruction;
                _player.Play(sound);
            }

            System.Diagnostics.Debug.WriteLine($"Trial stimulus: {CurrentSetup.Stimuli[_targetIndexes[_trialIndex]].Text}");
        }
        else if (_state == State.Info)
        {
            _timer.Interval = Math.Max(1, _settings.InfoDuration);
            _timer.Start();

            var stimulus = CurrentSetup.Stimuli[_targetIndexes[_trialIndex]];

            bool isCorrect = stimulus?.WasActivated ?? false;
            _logger.Add(LogSource.Stimuli, LogAction.Hidden);
            _logger.Add(LogSource.Experiment, LogAction.Result, isCorrect ? "success" : "failure");

            _server.Send($"RES {stimulus?.Text} {isCorrect}");

            if (_settings.InfoDuration > 0)
            {
                StimuliHidden?.Invoke(this, isCorrect);
                _player.Play(isCorrect ? SOUND_CORRECT : SOUND_INCORRECT);
            }
        }
        else if (_state == State.Inactive)
        {
            CurrentSetup = null;

            _server.Send($"FIN");
            _logger.Add(LogSource.Experiment, LogAction.Stop);

            SystemSounds.Beep.Play();
        }
    }

    private int[] PrepareTargets(Setup setup)
    {
        List<int> indexes = [];
        while (indexes.Count < _settings.TrialCount)
        {
            indexes.AddRange(setup.Stimuli.Select((btn, i) => i));
        }

        indexes.RemoveRange(_settings.TrialCount, indexes.Count - _settings.TrialCount);

        Span<int> shuffledIndexes = indexes.ToArray();
        _random.Shuffle(shuffledIndexes);

        return shuffledIndexes.ToArray();
    }

    private void UpdateBackgroundNoiseState()
    {
        if (_settings.PlayBackgroundNoise && !_backgroundSound.IsPlaying)
        {
            _backgroundSound.Play(true);
        }
        else if (!_settings.PlayBackgroundNoise && _backgroundSound.IsPlaying)
        {
            _backgroundSound.Stop();
        }
    }

    // Event handlers

    private void Server_Data(object? sender, string e)
    {
        if (e.Equals(NET_COMMAND_START, _stringComparison))
        {
            if (!IsRunning)
                Run(_settings.SetupIndex);
        }
        else if (e.Equals(NET_COMMAND_STOP, _stringComparison))
        {
            if (IsRunning)
                Stop();
        }
        else if (e.StartsWith(NET_COMMAND_SET_TASK, _stringComparison))
        {
            if (!IsRunning && int.TryParse(e.Substring(NET_COMMAND_SET_TASK.Length).Trim(), out int setupIndex) &&
                setupIndex >= 0 && setupIndex < _setups.Count)
            {
                SetupRequested?.Invoke(this, setupIndex);
            }
        }
        else if (e.Equals(NET_COMMAND_EXIT, _stringComparison))
        {
            if (IsRunning)
                Stop();
            App.Current.Shutdown();
        }
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_state == State.BlankScreen)
        {
            UpdateState(State.Stimuli);
        }
        else if (_state == State.Stimuli)
        {
            UpdateState(State.Info);
        }
        else if (_state == State.Info)
        {
            Next();
        }
    }
}
