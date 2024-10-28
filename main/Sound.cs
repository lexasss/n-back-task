using NAudio.Wave;
using System.IO;

namespace NBackTask;

/// <summary>
/// Plays an MP3 resource file
/// </summary>
internal class Sound
{
    /// <summary>
    /// Fires when the sound stops playing
    /// </summary>
    public event EventHandler Finished = delegate { };

    /// <summary>
    /// True if the sound is playing or paused
    /// </summary>
    public bool IsPlaying => _player.PlaybackState != PlaybackState.Stopped;

    /// <summary>
    /// Name, for debugging purposes
    /// </summary>
    public string Name { get; }

    public Sound(string filename, string name, string deviceName = "")
    {
        _mp3 = new Mp3FileReader(filename);

        Name = name;

        if (!string.IsNullOrEmpty(deviceName))
        {
            SetDevice(deviceName);
        }

        _player.Init(_mp3);
        _player.PlaybackStopped += OnPlaybackStopped;
    }

    /// <summary>
    /// Plays the sound
    /// </summary>
    /// <param name="cyclic">True if the sound will play in a loop and will be stopped externally, otherwise it will stop automatically after playing the sound once</param>
    /// <returns>Self to chain with <see cref="Chain(Action)"/> if needed</returns>
    public Sound Play(bool cyclic = false)
    {
        _cyclic = cyclic;
        _player.Volume = 1;
        _player.Play();
        //Utils.DispatchOnce.Do(0.05, () => _player.Play());

        return this;
    }

    public Sound Play(float volume)
    {
        _player.Volume = volume;
        _player.Play();
        //Utils.DispatchOnce.Do(0.05, () => _player.Play());

        return this;
    }

    public Sound PlayASAP()
    {
        _cyclic = false;
        _player.Volume = 1;
        _player.Play();

        return this;
    }

    /// <summary>
    /// Registers a callback to execute after the playback stops automatically.
    /// </summary>
    /// <param name="onFinished">A callback function</param>
    public void Chain(Action onFinished)
    {
        _onFinished.Add(onFinished);
    }

    /// <summary>
    /// Stops playing, cancels all chaining actions
    /// </summary>
    public void Stop()
    {
        _onFinished.Clear();
        _cyclic = false;

        if (IsPlaying)
        {
            _player.Stop();
        }
    }

    // Internal

    readonly WaveOut _player = new();
    readonly Mp3FileReader _mp3;

    readonly List<Action> _onFinished = [];

    bool _cyclic = false;

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _mp3.Seek(0, SeekOrigin.Begin);

        if (_cyclic)
        {
            Play(true);
        }
        else
        {
            Finished(this, new EventArgs());

            if (_onFinished.Count > 0)
            {
                _onFinished[0]();
                _onFinished.RemoveAt(0);
            }
        }
    }

    private void SetDevice(string deviceName)
    {
        int deviceID = -1;
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            if (caps.ProductName.Contains("Speakers") && caps.ProductName.Contains(deviceName))
            {
                deviceID = i;
                break;
            }
        }

        if (deviceID < 0)
        {
            throw new ArgumentException($"Device '{deviceName}' is not connected");
        }
        else
        {
            _player.DeviceNumber = deviceID;
        }
    }
}

internal class Player
{
    public static string SoundsFolder => "assets/sounds";
    public static string AudioType => "mp3";

    public static int NumberOfInstructions => Directory.GetFiles(SoundsFolder, $"*.{AudioType}").Length;

    public Player()
    {
        var mp3Files = Directory.GetFiles(SoundsFolder, $"*.{AudioType}");
        foreach (var item in mp3Files)
        {
            var name = Path.GetFileNameWithoutExtension(item);
            try
            {
                _sounds.Add(name, new Sound($"{SoundsFolder}/{name}.{AudioType}", name));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot load audio file {name}.{AudioType}:\n{ex.Message}");
            }
        }
    }

    public bool CheckSoundsExist(string[] names)
    {
        bool result = true;

        foreach (var name in names)
        {
            if (!_sounds.ContainsKey(name))
            {
                result = false;
                System.Diagnostics.Debug.Write($"No such audio file: '{name}.{AudioType}'");
            }
        }

        return result;
    }

    public void Play(string name)
    {
        _sounds[name].Play();
    }

    // Internal

    readonly Dictionary<string, Sound> _sounds = [];
}