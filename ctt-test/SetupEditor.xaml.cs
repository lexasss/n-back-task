using System.Windows;

namespace CttTest;

public partial class SetupEditor : Window
{
    public IEnumerable<string> Setups => _procedure.Setups.Select(setup => setup.Name);

    public int SelectedSetupIndex
    {
        get => _selectedSetupIndex;
        set
        {
            _selectedSetupIndex = value;
            LoadSetup(_selectedSetupIndex);
        }
    }

    internal SetupEditor(Procedure procedure)
    {
        InitializeComponent();

        _procedure = procedure;
        DataContext = this;
    }

    // Internal

    readonly Procedure _procedure;

    int _selectedSetupIndex = 0;

    private void LoadSetup(int index)
    {
        var setup = _procedure.Setups[index];
    }

    // UI

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
