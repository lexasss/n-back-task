using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace NBackTask;

internal class FontSizeController : INotifyPropertyChanged
{
    public int RowsInLayout { get; set; } = 1;
    public Binding Binding { get; }

    public double FontSize => Math.Max(6, Math.Min(((_container.ActualHeight - 36) / RowsInLayout - 16) * 0.8, Math.Min(_stimulusSize - 10, _maxSize)));

    public event PropertyChangedEventHandler? PropertyChanged;

    public FontSizeController(Control container, double maxSize = 180)
    {
        _container = container;
        _container.SizeChanged += Container_SizeChanged;

        _maxSize = maxSize;

        Binding = new Binding(nameof(FontSize));
        Binding.Source = this;
    }

    public void SetStimulusSize(double value)
    {
        if (_stimulusSize != value)
        {
            _stimulusSize = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontSize)));
        }
    }

    // Internal

    readonly Control _container;
    readonly double _maxSize;

    double _stimulusSize = Settings.Instance.StimulusUnstretchedSize;

    private void Container_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontSize)));
    }
}
