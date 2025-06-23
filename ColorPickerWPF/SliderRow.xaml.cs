using System;
using System.Windows;
using System.Windows.Controls;

namespace ColorPickerWPF;

public partial class SliderRow : UserControl
{
    public class ValueChangedEventArgs(double value) : EventArgs
    {
        public double Value => value;
    }

    public event EventHandler<ValueChangedEventArgs> ValueChanged;

    public string FormatString { get; set; }

    protected bool UpdatingValues = false;

    public SliderRow()
    {
        FormatString = "F2";

        InitializeComponent();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Set textbox
        var value = Slider.Value;

        if (!UpdatingValues)
        {
            UpdatingValues = true;
            TextBox.Text = value.ToString(FormatString);
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(value));
            UpdatingValues = false;
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!UpdatingValues)
        {
            var text = TextBox.Text;

            bool ok = double.TryParse(text, out double parsedValue);
            if (ok)
            {
                UpdatingValues = true;
                Slider.Value = parsedValue;
                ValueChanged?.Invoke(this, new ValueChangedEventArgs(parsedValue));
                UpdatingValues = false;
            }
        }
    }
}