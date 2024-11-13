namespace NBackTask;

public class Stimulus(string text, /*int? size = null, */string? audioInstruction = null)
{
    public string Text { get; } = text;
    public string AudioInstruction { get; } = audioInstruction ?? text;
    // public int Size { get; } = size ?? Settings.Instance.StimulusUnstretchedSize;

    public bool WasActivated { get; set; } = false;
}
