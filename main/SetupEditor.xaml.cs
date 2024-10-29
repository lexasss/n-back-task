using System.ComponentModel;
using System.Windows;

namespace NBackTask;

public partial class SetupEditor : Window, INotifyPropertyChanged
{
    public IEnumerable<string> SetupNames => _setups.Select(setup => setup.Name);
    public bool CanDeleteSetup => SetupNames.Count() > 1;
    public SetupData? SetupData => _selectedSetupIndex < 0 ? null : _setups[_selectedSetupIndex];

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

    internal SetupEditor(List<SetupData> setups, int currentIndex)
    {
        InitializeComponent();

        _setups = setups;
        _selectedSetupIndex = currentIndex;

        DataContext = this;
    }

    // Internal

    readonly List<SetupData> _setups;

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

    private void CreateSetup_Click(object sender, RoutedEventArgs e)
    {
        var setupName = InputDialog.ShowDialog("Enter the setup name:");
        if (setupName != null)
        {
            if (SetupNames.FirstOrDefault(item => item == setupName) is string)
            {
                MessageBox.Show($"Setup with name '{setupName}' exist already. Aborted.", "N-Back task", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _setups.Add(_setups[_selectedSetupIndex] with { Name = setupName });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SetupNames)));

            SelectedSetupIndex = _setups.Count - 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSetupIndex)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDeleteSetup)));
        }
    }

    private void DeleteSetup_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show($"Are you sure to delete the setup '{_setups[_selectedSetupIndex].Name}'?", "N-Back task", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            var indexToRemove = _selectedSetupIndex;

            _setups.RemoveAt(_selectedSetupIndex);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SetupNames)));

            SelectedSetupIndex = Math.Min(indexToRemove, _setups.Count - 1);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSetupIndex)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDeleteSetup)));
        }
    }
}
