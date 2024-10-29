using System.ComponentModel;
using System.Windows;

namespace NBackTask;

public partial class SetupEditor : Window, INotifyPropertyChanged
{
    public IEnumerable<string> SetupNames { get; }
    public SetupData SetupData => _setups[_selectedSetupIndex];

    public event PropertyChangedEventHandler? PropertyChanged;

    public int SelectedSetupIndex
    {
        get => _selectedSetupIndex;
        set
        {
            _selectedSetupIndex = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SetupData)));
        }
    }

    internal SetupEditor(SetupData[] setups, int currentIndex)
    {
        InitializeComponent();

        _setups = setups;
        _selectedSetupIndex = currentIndex;

        SetupNames = setups.Select(setup => setup.Name);

        DataContext = this;
    }

    // Internal

    readonly SetupData[] _setups;

    int _selectedSetupIndex = 0;

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var numOfInstructions = Player.NumberOfInstructions;
        foreach (SetupData setup in _setups)
        {
            var targetCount = setup.ColumnCount * setup.RowCount;
            if (targetCount > numOfInstructions)
            {
                MessageBox.Show($"This app has only {numOfInstructions} audio instructions, but the setup '{setup.Name}' has {targetCount} targets.\n\n" +
                    $"Please either select less rows/column for this setup, or add more {Player.AudioType} instructions to the folder '{Player.SoundsFolder}'",
                    "N-Back task", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        
        DialogResult = true;
    }
}
