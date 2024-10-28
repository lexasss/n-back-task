namespace NBackTask;

public class Stimulus(string text, int size = 152, string? audioInstruction = null)
{
    public string Text { get; } = text;
    public string AudioInstruction { get; } = audioInstruction ?? text;
    public int Size { get; } = size;

    public bool WasActivated { get; set; } = false;
}
