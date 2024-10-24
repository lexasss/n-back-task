using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace CttTest;

internal class Procedure
{
    public Setup[] Setups { get; } = [];
    public Setup? CurrentSetup { get; private set; } = null;

    public bool IsRunning => _state != State.Inactive;

    public event EventHandler<Setup>? NextTask;
    public event EventHandler? StimuliShown;
    public event EventHandler<bool>? StimuliHidden;
    public event EventHandler? Finished;

    public Procedure()
    {
        _timer.AutoReset = false;
        _timer.Elapsed += Timer_Elapsed;

        var setupTypes = Setup.GetAllTypes();
        Setups = setupTypes.Select(type => Activator.CreateInstance(type) as Setup).Where(setup => setup != null).ToArray()!;

        var audioFiIlename = Setups.SelectMany(setup => setup.Stimuli.Select(stimulus => stimulus.AudioInstruction)).ToList();
        audioFiIlename.Add(SOUND_CORRECT);
        audioFiIlename.Add(SOUND_INCORRECT);

        _player.CheckSoundsExist(audioFiIlename.ToArray());
    }

    public void Run(int setupIndex)
    {
        if (_state != State.Inactive)
            return;

        if (setupIndex < 0 || setupIndex >= Setups.Length)
            return;

        CurrentSetup = Setups[setupIndex];

        _logger.Reset();
        _logger.Add("experiment", "start", CurrentSetup.Name);

        _targetIndexes = CurrentSetup.PrepareTargets();

        _taskIndex = -1;

        Next();
    }

    public void Stop()
    {
        if (_state == State.Inactive)
            return;

        _timer.Stop();

        UpdateState(State.Inactive);
    }

    public bool CanActivateStimulus(Stimulus? stimulus)
    {
        var canActivate = CurrentSetup?.GetActiveStimulus() is null;

        if (canActivate && stimulus != null)
        {
            stimulus.WasActivated = true;
            _logger.Add("stimulus", "activated", stimulus.Text);
        }

        return canActivate;
    }

    public void DeactivateStimulus()
    {
        _timer.Stop();
        UpdateState(State.Info);
    }

    public void LogStimuliOrder(Stimulus[] stimuli)
    {
        _logger.Add("stimuli", "order", string.Join(' ', stimuli.Select(s => s.Text)));
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

    State _state = State.Inactive;
    int[] _targetIndexes = [];
    int _taskIndex = -1;

    private void Next()
    {
        if (CurrentSetup == null)
            return;

        CurrentSetup.ResetStimuli();

        if (++_taskIndex < CurrentSetup.TaskCount)
        {
            UpdateState(State.BlankScreen);
        }
        else
        {
            Stop();
            Finished?.Invoke(this, EventArgs.Empty);

            var filename = _logger.Save();
            if (filename != null)
            {
                MessageBox.Show($"Data saved to '{filename}'", "CTT test", MessageBoxButton.OK, MessageBoxImage.Information);
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
            _timer.Interval = Settings.BlankScreenDuration;
            _timer.Start();

            _logger.Add("stimuli", "target", CurrentSetup.Stimuli[_targetIndexes[_taskIndex]]?.Text ?? "?");

            NextTask?.Invoke(this, CurrentSetup);

        }
        else if (_state == State.Stimuli)
        {
            _timer.Interval = Settings.StimulusDuration;
            _timer.Start();

            StimuliShown?.Invoke(this, EventArgs.Empty);

            _logger.Add("stimuli", "displayed");

            var stimulus = CurrentSetup.Stimuli[_targetIndexes[_taskIndex]];
            var sound = stimulus?.AudioInstruction;
            if (sound != null)
            {
                _player.Play(sound);
            }

            System.Diagnostics.Debug.WriteLine($"Task stimulus: {CurrentSetup.Stimuli[_targetIndexes[_taskIndex]].Text}");
        }
        else if (_state == State.Info)
        {
            _timer.Interval = Settings.InfoDuration;
            _timer.Start();

            var stimulus = CurrentSetup.Stimuli[_targetIndexes[_taskIndex]];

            bool isCorrect = stimulus?.WasActivated ?? false;
            _logger.Add("stimuli", "hidden");
            _logger.Add("experiment", "result", isCorrect ? "success" : "failure");

            StimuliHidden?.Invoke(this, isCorrect);

            _player.Play(isCorrect ? SOUND_CORRECT : SOUND_INCORRECT);

            System.Diagnostics.Debug.WriteLine($"Is correct: {isCorrect}");
        }
        else if (_state == State.Inactive)
        {
            CurrentSetup = null;

            _logger.Add("experiment", "stop");
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
