using System.Text.Json;
using System.Windows;

namespace CttTest;

public enum StimuliOrder
{
    Ordered,
    Randomized
}

public class SetupData
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

public class Setup
{
    public string Name { get; protected set; }
    public int RowCount { get; protected set; }
    public int ColumnCount { get; protected set; }
    public HorizontalAlignment Alignment { get; protected set; }
    public StimuliOrder StimuliOrder { get; protected set; }

    public int TrialCount { get; set; }

    public Stimulus[] Stimuli => _stimuli.ToArray();

    public static Type[] GetAllTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
        .Where(type => type.IsSubclassOf(typeof(Setup))).ToArray();

    public Setup(SetupData data)
    {
        Name = data.Name;
        RowCount = data.RowCount;
        ColumnCount = data.ColumnCount;
        StimuliOrder = data.StimuliOrder;
        Alignment = data.Alignment;

        TrialCount = Settings.Instance.TrialCount;

        CreateStimuli();
    }

    public Setup(string name, int rowCount, int columnCount, HorizontalAlignment alignment, StimuliOrder order)
    {
        Name = name;
        RowCount = rowCount;
        ColumnCount = columnCount;
        StimuliOrder = order;
        Alignment = alignment;

        var settings = Properties.Settings.Default;
        if (!string.IsNullOrEmpty(settings.Setups))
        {
            var setups = JsonSerializer.Deserialize<SetupData[]>(settings.Setups);
            var thisSetup = setups?.FirstOrDefault(setup => setup.Name == Name);
            if (thisSetup != null)
            {
                RowCount = thisSetup.RowCount;
                ColumnCount = thisSetup.ColumnCount;
                Alignment = thisSetup.Alignment;
                StimuliOrder = thisSetup.StimuliOrder;
            }
        }

        TrialCount = Settings.Instance.TrialCount;

        CreateStimuli();
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

    private void CreateStimuli()
    {
        for (int i = 1; i <= RowCount * ColumnCount; i++)
        {
            _stimuli.Add(new Stimulus($"{i % 10}"));
        }
    }
}

internal class SetupVeryEasy : Setup
{
    public SetupVeryEasy() : base("Very Easy", 1, 2, HorizontalAlignment.Stretch, StimuliOrder.Ordered) { }
}

internal class SetupEasy : Setup
{
    public SetupEasy() : base("Easy", 2, 2, HorizontalAlignment.Stretch, StimuliOrder.Ordered) { }
}

internal class SetupModerate : Setup
{
    public SetupModerate() : base("Moderate", 2, 5, HorizontalAlignment.Stretch, StimuliOrder.Ordered) { }
}

internal class SetupHard : Setup
{
    public SetupHard() : base("Hard", 2, 2, HorizontalAlignment.Stretch, StimuliOrder.Randomized) { }
}

internal class SetupVeryHard : Setup
{
    public SetupVeryHard() : base("Very Hard", 2, 5, HorizontalAlignment.Stretch, StimuliOrder.Randomized) { }
}