using ColorPickerWPF.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using ColorPalette = ColorPickerWPF.Code.ColorPalette;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace ColorPickerWPF;

public partial class ColorPickerControl : UserControl
{
    public Color Color = Colors.White;

    public delegate void ColorPickerChangeHandler(Color color);

    public event ColorPickerChangeHandler OnPickColor;

    internal List<ColorSwatchItem> ColorSwatch1 = new List<ColorSwatchItem>();
    internal List<ColorSwatchItem> ColorSwatch2 = new List<ColorSwatchItem>();

    public bool IsSettingValues = false;

    protected const int NumColorsFirstSwatch = 39;
    protected const int NumColorsSecondSwatch = 112;

    internal static ColorPalette ColorPalette;
    
    private FormatConvertedBitmap _colorPicker2;
    private byte[] _colorPicker2_Data;
    private WriteableBitmap _colorPicker2Output;

    public ColorPickerControl()
    {
        InitializeComponent();

        ColorPickerSwatch.ColorPickerControl = this;

        // Load from file if possible
        if (ColorPickerSettings.UsingCustomPalette && File.Exists(ColorPickerSettings.CustomPaletteFilename))
        {
            try
            {
                ColorPalette = ColorPalette.LoadFromXml(ColorPickerSettings.CustomPaletteFilename);
            }
            catch { }
        }

        if (ColorPalette == null)
        {
            ColorPalette = new ColorPalette();
            ColorPalette.InitializeDefaults();
        }

        ColorSwatch1.AddRange(ColorPalette.BuiltInColors.Take(NumColorsFirstSwatch).ToArray());

        ColorSwatch2.AddRange(ColorPalette.BuiltInColors.Skip(NumColorsFirstSwatch).Take(NumColorsSecondSwatch).ToArray());

        Swatch1.SwatchListBox.ItemsSource = ColorSwatch1;
        Swatch2.SwatchListBox.ItemsSource = ColorSwatch2;

        if (ColorPickerSettings.UsingCustomPalette)
        {
            CustomColorSwatch.SwatchListBox.ItemsSource = ColorPalette.CustomColors;
        }
        else
        {
            customColorsLabel.Visibility = Visibility.Collapsed;
            CustomColorSwatch.Visibility = Visibility.Collapsed;
        }

        RSlider.Slider.Maximum = 255;
        GSlider.Slider.Maximum = 255;
        BSlider.Slider.Maximum = 255;
        ASlider.Slider.Maximum = 255;
        HSlider.Slider.Maximum = 360;
        SSlider.Slider.Maximum = 1;
        LSlider.Slider.Maximum = 1;

        RSlider.Label.Content = "R";
        RSlider.Slider.TickFrequency = 1;
        RSlider.Slider.IsSnapToTickEnabled = true;
        GSlider.Label.Content = "G";
        GSlider.Slider.TickFrequency = 1;
        GSlider.Slider.IsSnapToTickEnabled = true;
        BSlider.Label.Content = "B";
        BSlider.Slider.TickFrequency = 1;
        BSlider.Slider.IsSnapToTickEnabled = true;

        ASlider.Label.Content = "A";
        ASlider.Slider.TickFrequency = 1;
        ASlider.Slider.IsSnapToTickEnabled = true;

        HSlider.Label.Content = "H";
        HSlider.Slider.TickFrequency = 1;
        HSlider.Slider.IsSnapToTickEnabled = true;
        SSlider.Label.Content = "S";
        //SSlider.Slider.TickFrequency = 1;
        //SSlider.Slider.IsSnapToTickEnabled = true;
        LSlider.Label.Content = "V";
        //LSlider.Slider.TickFrequency = 1;
        //LSlider.Slider.IsSnapToTickEnabled = true;

        SetColor(Color);
    }

    public void SetColor(Color color)
    {
        Color = color;

        CustomColorSwatch.CurrentColor = color;

        IsSettingValues = true;

        RSlider.Slider.Value = Color.R;
        GSlider.Slider.Value = Color.G;
        BSlider.Slider.Value = Color.B;
        ASlider.Slider.Value = Color.A;

        SSlider.Slider.Value = Color.GetSaturation();
        LSlider.Slider.Value = Color.GetBrightness();
        HSlider.Slider.Value = Color.GetHue();

        ColorDisplayBorder.Background = new SolidColorBrush(Color);

        HexBox.Text = Util.ToHexStringWithoutAlpha(Color);

        IsSettingValues = false;
        OnPickColor?.Invoke(color);
    }

    internal void CustomColorsChanged()
    {
        if (ColorPickerSettings.UsingCustomPalette)
        {
            SaveCustomPalette(ColorPickerSettings.CustomPaletteFilename);
        }
    }

    protected void SampleImageClick(BitmapSource img, Point pos)
    {
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/82a5731e-e201-4aaf-8d4b-062b138338fe/getting-pixel-information-from-a-bitmapimage?forum=wpf

        int stride = (int)img.Width * 4;
        int size = (int)img.Height * stride;
        byte[] pixels = new byte[size];

        img.CopyPixels(pixels, stride, 0);

        // Get pixel
        var x = (int)pos.X;
        var y = (int)pos.Y;

        int index = y * stride + 4 * x;

        byte red = pixels[index];
        byte green = pixels[index + 1];
        byte blue = pixels[index + 2];
        byte alpha = pixels[index + 3];

        var color = Color.FromArgb(alpha, blue, green, red);
        SetColor(color);
    }

    private void SampleImage_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this);

        MouseMove += ColorPickerControl_MouseMove;
        MouseUp += ColorPickerControl_MouseUp;
    }

    private void SampleImage2_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this);

        MouseMove += ColorPickerControl2_MouseMove;
        MouseUp += ColorPickerControl2_MouseUp;
    }

    private void ColorPickerControl_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(SampleImage);
        var img = SampleImage.Source as BitmapSource;

        if (pos.X > 0 && pos.Y > 0 && pos.X < img.PixelWidth && pos.Y < img.PixelHeight)
            SampleImageClick(img, pos);
    }

    private void ColorPickerControl2_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(SampleImage2);
        var img = SampleImage2.Source as BitmapSource;

        if (pos.X > 0 && pos.Y > 0 && pos.X < img.PixelWidth && pos.Y < img.PixelHeight)
            SampleImageClick(img, pos);
    }

    private void ColorPickerControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        MouseMove -= ColorPickerControl_MouseMove;
        MouseUp -= ColorPickerControl_MouseUp;
    }
    private void ColorPickerControl2_MouseUp(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        MouseMove -= ColorPickerControl2_MouseMove;
        MouseUp -= ColorPickerControl2_MouseUp;
    }

    private void Swatch_PickColor(Color color)
    {
        SetColor(color);
    }

    private void HSlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var s = Color.GetSaturation();
            var l = Color.GetBrightness();
            var h = (float)value;
            var a = (int)ASlider.Slider.Value;
            Color = Util.FromAhsb(a, h, s, l);

            SetColor(Color);
        }
    }

    private void RSlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var val = (byte)value;
            Color.R = val;
            SetColor(Color);
        }
    }

    private void GSlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var val = (byte)value;
            Color.G = val;
            SetColor(Color);
        }
    }

    private void BSlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var val = (byte)value;
            Color.B = val;
            SetColor(Color);
        }
    }

    private void ASlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var val = (byte)value;
            Color.A = val;
            SetColor(Color);
        }
    }

    private void SSlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var s = (float)value;
            var l = Color.GetBrightness();
            var h = Color.GetHue();
            var a = (int)ASlider.Slider.Value;
            Color = Util.FromAhsb(a, h, s, l);

            SetColor(Color);
        }
    }

    private void PickerHueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateImageForHSV();
    }

    private void UpdateImageForHSV()
    {
        //var hueChange = (int)((PickerHueSlider.Value / 360.0) * 240);
        var sliderHue = (float)PickerHueSlider.Value;

        if (_colorPicker2 == null)
        {
            LoadColorPicker2();
        }

        if (sliderHue <= 0f || sliderHue >= 360f)
        {
            // No hue change just return
            return;
        }

        var imagebuffer = new byte[_colorPicker2_Data.Length];

        // Copy the image data from our color picker into the array for our output image, so we dont change the values of the original image.
        Array.Copy(_colorPicker2_Data, imagebuffer, imagebuffer.Length);

        for (int x = 0; x < _colorPicker2.PixelWidth; x++)
        {
            for (int y = 0; y < _colorPicker2.PixelHeight; y++)
            {
                var offset = (y * _colorPicker2.PixelWidth + x) * 4;

                System.Drawing.Color pixel;

                pixel = System.Drawing.Color.FromArgb(255, imagebuffer[offset + 2],
                    imagebuffer[offset + 1], imagebuffer[offset]);

                var newHue = (float)(sliderHue + pixel.GetHue());
                if (newHue >= 360) newHue -= 360;

                var color = Util.FromAhsb((int)255, newHue, pixel.GetSaturation(), pixel.GetBrightness());

                imagebuffer[offset + 0] = color.B;
                imagebuffer[offset + 1] = color.G;
                imagebuffer[offset + 2] = color.R;
                imagebuffer[offset + 3] = color.A;
            }
        }

        // Copy back the changed pixels to the image
        _colorPicker2Output.Lock();
        Marshal.Copy(imagebuffer, 0, _colorPicker2Output.BackBuffer, imagebuffer.Length);
        _colorPicker2Output.AddDirtyRect(new Int32Rect(0, 0, _colorPicker2Output.PixelWidth, _colorPicker2.PixelHeight));
        _colorPicker2Output.Unlock();
    }

    private void LoadColorPicker2()
    {
        //Load the embedded resource
        BitmapImage image = new BitmapImage(new Uri("pack://application:,,,/ColorPickerWPF;component/Resources/colorpicker2.png", UriKind.Absolute));

        _colorPicker2 = new FormatConvertedBitmap();
        _colorPicker2.BeginInit();
        _colorPicker2.Source = image;
        _colorPicker2.DestinationFormat = PixelFormats.Pbgra32;
        _colorPicker2.EndInit();

        _colorPicker2_Data = new byte[_colorPicker2.PixelWidth * _colorPicker2.PixelHeight * 4];
        _colorPicker2.CopyPixels(_colorPicker2_Data, _colorPicker2.PixelWidth * 4, 0);

        _colorPicker2Output = new WriteableBitmap(_colorPicker2.PixelWidth, _colorPicker2.PixelHeight, 96, 96, PixelFormats.Pbgra32, null);
        SampleImage2.Source = _colorPicker2Output;
    }

    private void OnPicker2Selected(object sender, RoutedEventArgs e)
    {
        PickerHueSlider.Value = Color.GetHue();
    }

    private void LSlider_ValueChanged(double value)
    {
        if (!IsSettingValues)
        {
            var s = Color.GetSaturation();
            var l = (float)value;
            var h = Color.GetHue();
            var a = (int)ASlider.Slider.Value;
            Color = Util.FromAhsb(a, h, s, l);

            SetColor(Color);
        }
    }

    public void SaveCustomPalette(string filename)
    {
        var colors = CustomColorSwatch.GetColors();
        ColorPalette.CustomColors = colors;

        try
        {
            ColorPalette.SaveToXml(filename);
        }
        catch { }
    }

    public void LoadCustomPalette(string filename)
    {
        if (File.Exists(filename))
        {
            try
            {
                ColorPalette = ColorPalette.LoadFromXml(filename);

                CustomColorSwatch.SwatchListBox.ItemsSource = ColorPalette.CustomColors.ToList();

                // Do regular one too

                ColorSwatch1.Clear();
                ColorSwatch2.Clear();
                ColorSwatch1.AddRange(ColorPalette.BuiltInColors.Take(NumColorsFirstSwatch).ToArray());
                ColorSwatch2.AddRange(ColorPalette.BuiltInColors.Skip(NumColorsFirstSwatch).Take(NumColorsSecondSwatch).ToArray());
                Swatch1.SwatchListBox.ItemsSource = ColorSwatch1;
                Swatch2.SwatchListBox.ItemsSource = ColorSwatch2;
            }
            catch { }
        }
    }

    public void LoadDefaultCustomPalette()
    {
        LoadCustomPalette(Path.Combine(ColorPickerSettings.CustomColorsDirectory, ColorPickerSettings.CustomColorsFilename));
    }

    private void HexBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!IsSettingValues)
        {
            try
            {
                if (ColorDisplayBorder != null)
                    SetColor(Util.ColorFromHexString(HexBox.Text));
            }
            catch { }
        }
    }
}