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
        _server.Data += (s, e) =>
        {
            if (e == "start")
            {
                if (!IsRunning)
                    Run(_settings.SetupIndex);
            }
            else if (e == "stop")
            {
                if (IsRunning)
                    Stop();
            }
        };

        _server.Start();

        UpdateBackgroundNoiseState();

        _settings.Updated += (s, e) => UpdateBackgroundNoiseState();

        Application.Current.Exit += (s, e) =>
        {
            _backgroundSound.Stop();
            SaveSetups();
        };
    }

    public void Run(int setupIndex)
    {
        if (_state != State.Inactive)
            return;

        if (setupIndex < 0 || setupIndex >= _setups.Count)
            return;

        CurrentSetup = _setups[setupIndex];

        _logger.Reset();
        _logger.Add("experiment", "start", CurrentSetup.Name);

        _server.Send($"STR");

        _targetIndexes = PrepareTargets(CurrentSetup);

        _trialIndex = -1;

        Started?.Invoke(this, EventArgs.Empty);

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
            _logger.Add("stimulus", "activated", stimulus.Text);
            _server.Send($"ACT {stimulus.Text}");
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
        _logger.Add("stimuli", "order", string.Join(' ', stimuli.Select(s => s.Text)));
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

    readonly System.Timers.Timer _timer = new();
    readonly Player _player = new();
    readonly Logger _logger = Logger.Instance;
    readonly Settings _settings = Settings.Instance;
    readonly Random _random = new();
    readonly List<Setup> _setups = [];
    readonly Sound _backgroundSound = new("assets/sounds/noise.mp3", "background");

    readonly TcpServer _server = new();

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
            _logger.Add("stimuli", "target", stimulus?.Text ?? "?");

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

            _logger.Add("stimuli", "displayed");

            var stimulus = CurrentSetup.Stimuli[_targetIndexes[_trialIndex]];
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
            _logger.Add("stimuli", "hidden");
            _logger.Add("experiment", "result", isCorrect ? "success" : "failure");

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
            _logger.Add("experiment", "stop");

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
