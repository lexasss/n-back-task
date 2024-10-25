using System.Windows;

namespace CttTest;

internal enum StimuliOrder
{
    Ordered,
    Randomized
}

internal class Setup
{
    public string Name { get; }
    public int RowCount { get; }
    public int ColumnCount { get; }
    public StimuliOrder StimuliOrder { get; }
    public HorizontalAlignment Alignment { get; }

    public int TrialCount { get; set; }

    public Stimulus[] Stimuli => _stimuli.ToArray();

    public static Type[] GetAllTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
        .Where(type => type.IsSubclassOf(typeof(Setup))).ToArray();

    public Setup(string name, int rowCount, int columnColumn, HorizontalAlignment alignment, StimuliOrder order, int taskCount)
    {
        Name = name;
        RowCount = rowCount;
        ColumnCount = columnColumn;
        TrialCount = taskCount;
        StimuliOrder = order;
        Alignment = alignment;

        for (int i = 1; i <= rowCount * columnColumn; i++)
        {
            _stimuli.Add(new Stimulus($"{i % 10}"));
        }
    }

    public virtual (int, int) GetStimulusLocation(int stimulusIndex)
    {
        return (stimulusIndex / ColumnCount, stimulusIndex % ColumnCount);
    }

    public void ResetStimuli()
    {
        foreach (Stimulus stimulus in _stimuli)
        {
            stimulus.WasActivated = false;
        }
    }

    public Stimulus? GetActiveStimulus()
    {
        foreach (Stimulus stimulus in _stimuli)
        {
            if (stimulus.WasActivated)
                return stimulus;
        }

        return null;
    }

    public int[] PrepareTargets()
    {
        List<int> indexes = [];
        while (indexes.Count < TrialCount)
        {
            indexes.AddRange(_stimuli.Select((btn, i) => i));
        }

        indexes.RemoveRange(TrialCount, indexes.Count - TrialCount);

        Span<int> shuffledIndexes = indexes.ToArray();
        _random.Shuffle(shuffledIndexes);

        return shuffledIndexes.ToArray();
    }

    public override string ToString() => Name;

    // Internal

    protected List<Stimulus> _stimuli = [];

    readonly Random _random = new();
}

internal class SetupVeryEasy : Setup
{
    public SetupVeryEasy() : base("Very Easy", 1, 2, HorizontalAlignment.Stretch, StimuliOrder.Ordered, Settings.Instance.TrialCount) { }
}

internal class SetupEasy : Setup
{
    public SetupEasy() : base("Easy", 2, 2, HorizontalAlignment.Stretch, StimuliOrder.Ordered, Settings.Instance.TrialCount) { }
}

internal class SetupModerate : Setup
{
    public SetupModerate() : base("Moderate", 2, 5, HorizontalAlignment.Stretch, StimuliOrder.Ordered, Settings.Instance.TrialCount) { }
}

internal class SetupHard : Setup
{
    public SetupHard() : base("Hard", 2, 2, HorizontalAlignment.Stretch, StimuliOrder.Randomized, Settings.Instance.TrialCount) { }
}

internal class SetupVeryHard : Setup
{
    public SetupVeryHard() : base("Very Hard", 2, 5, HorizontalAlignment.Stretch, StimuliOrder.Randomized, Settings.Instance.TrialCount) { }
}