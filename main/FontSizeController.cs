using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace NBackTask;

internal class FontSizeController : INotifyPropertyChanged
{
    public double StimulusSize { get; set; } = 100;
    public int RowsInLayout { get; set; } = 1;
    public Binding Binding { get; }

    public double FontSize => Math.Min(((_container.ActualHeight - 36) / RowsInLayout - 16) * 0.8, Math.Min(StimulusSize - 10, _maxSize));

    public event PropertyChangedEventHandler? PropertyChanged;

    public FontSizeController(Control container, double maxSize = 180)
    {
        _container = container;
        _container.SizeChanged += Container_SizeChanged;

        _maxSize = maxSize;

        Binding = new Binding(nameof(FontSize));
        Binding.Source = this;
    }

    // Internal

    readonly Control _container;
    readonly double _maxSize;

    private void Container_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontSize)));
    }
}
