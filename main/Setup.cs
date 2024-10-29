using System.Windows;

namespace NBackTask;

public enum StimuliOrder
{
    Ordered,
    Randomized
}

public interface ISetup
{
    public string Name { get; }
    public int RowCount { get; }
    public int ColumnCount { get; }
    public StimuliOrder StimuliOrder { get; }
    public HorizontalAlignment Alignment { get; }
}

public record class SetupData : ISetup
{
    public string Name { get; init; } = "";
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public StimuliOrder StimuliOrder { get; set; }
    public HorizontalAlignment Alignment { get; set; }

    public static SetupData From(Setup setup) => new()
    {
        Name = setup.Name,
        RowCount = setup.RowCount,
        ColumnCount = setup.ColumnCount,
        Alignment = setup.Alignment,
        StimuliOrder = setup.StimuliOrder,
    };
}

public class Setup : ISetup
{
    public string Name { get; }
    public int RowCount { get; }
    public int ColumnCount { get; }
    public HorizontalAlignment Alignment { get; }
    public StimuliOrder StimuliOrder { get; }

    public Stimulus[] Stimuli => _stimuli.ToArray();

    public Setup(ISetup data) : this(data.Name, data.RowCount, data.ColumnCount, data.Alignment, data.StimuliOrder) { }

    public Setup(string name, int rowCount, int columnCount, HorizontalAlignment alignment, StimuliOrder order)
    {
        Name = name;
        RowCount = rowCount;
        ColumnCount = columnCount;
        StimuliOrder = order;
        Alignment = alignment;

        CreateStimuli();
    }

    /// <summary>
    /// Computes the row and column of a stimulus
    /// </summary>
    /// <param name="stimulusIndex">Stimulus index</param>
    /// <returns>row and column</returns>
    public (int, int) GetStimulusLocation(int stimulusIndex)
    {
        return (stimulusIndex / ColumnCount, stimulusIndex % ColumnCount);
    }

    /// <summary>
    /// Remove activation marks from all stimuli
    /// </summary>
    public void ResetStimuli()
    {
        foreach (Stimulus stimulus in _stimuli)
        {
            stimulus.WasActivated = false;
        }
    }

    /// <summary>
    /// Return the stimuli marked as active (clicked)
    /// </summary>
    /// <returns>The active stimuli</returns>
    public Stimulus? GetActiveStimulus()
    {
        foreach (Stimulus stimulus in _stimuli)
        {
            if (stimulus.WasActivated)
                return stimulus;
        }

        return null;
    }

    public override string ToString() => Name;

    // Internal

    protected List<Stimulus> _stimuli = [];

    private void CreateStimuli()
    {
        for (int i = 1; i <= RowCount * ColumnCount; i++)
        {
            _stimuli.Add(new Stimulus($"{i}"));
        }
    }
}
