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
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace ColorPickerWPF;

public partial class ColorPickerControl : UserControl
{
    public Color Color => _color;

    public event EventHandler<Color> ColorPicked;

    public ColorPickerControl()
    {
        InitializeComponent();

        ColorPickerSwatch.ColorPickerControl = this;

        // Load from file if possible
        if (ColorPickerSettings.UsingCustomPalette && File.Exists(ColorPickerSettings.CustomPaletteFilename))
        {
            try
            {
                _colorPalette = ColorPalette.LoadFromXml(ColorPickerSettings.CustomPaletteFilename);
            }
            catch { }
        }

        if (_colorPalette == null)
        {
            _colorPalette = new ColorPalette();
            _colorPalette.InitializeDefaults();
        }

        _colorSwatch1.AddRange(_colorPalette.BuiltInColors.Take(NumColorsFirstSwatch).ToArray());

        _colorSwatch2.AddRange(_colorPalette.BuiltInColors.Skip(NumColorsFirstSwatch).Take(NumColorsSecondSwatch).ToArray());

        swtSwatch1.SwatchListBox.ItemsSource = _colorSwatch1;
        swtSwatch2.SwatchListBox.ItemsSource = _colorSwatch2;

        if (ColorPickerSettings.UsingCustomPalette)
        {
            swtCustomColors.SwatchListBox.ItemsSource = _colorPalette.CustomColors;
        }
        else
        {
            lblCustomColors.Visibility = Visibility.Collapsed;
            swtCustomColors.Visibility = Visibility.Collapsed;
        }

        sldR.Slider.Maximum = 255;
        sldG.Slider.Maximum = 255;
        sldB.Slider.Maximum = 255;
        slrA.Slider.Maximum = 255;
        sldH.Slider.Maximum = 360;
        sldS.Slider.Maximum = 1;
        sldL.Slider.Maximum = 1;

        sldR.Label.Content = "R";
        sldR.Slider.TickFrequency = 1;
        sldR.Slider.IsSnapToTickEnabled = true;
        sldG.Label.Content = "G";
        sldG.Slider.TickFrequency = 1;
        sldG.Slider.IsSnapToTickEnabled = true;
        sldB.Label.Content = "B";
        sldB.Slider.TickFrequency = 1;
        sldB.Slider.IsSnapToTickEnabled = true;

        slrA.Label.Content = "A";
        slrA.Slider.TickFrequency = 1;
        slrA.Slider.IsSnapToTickEnabled = true;

        sldH.Label.Content = "H";
        sldH.Slider.TickFrequency = 1;
        sldH.Slider.IsSnapToTickEnabled = true;
        sldS.Label.Content = "S";
        //SSlider.Slider.TickFrequency = 1;
        //SSlider.Slider.IsSnapToTickEnabled = true;
        sldL.Label.Content = "V";
        //LSlider.Slider.TickFrequency = 1;
        //LSlider.Slider.IsSnapToTickEnabled = true;

        SetColor(_color);
    }

    public void SetColor(Color color)
    {
        _color = color;

        swtCustomColors.CurrentColor = color;

        _isSettingValues = true;

        sldR.Slider.Value = _color.R;
        sldG.Slider.Value = _color.G;
        sldB.Slider.Value = _color.B;
        slrA.Slider.Value = _color.A;

        sldS.Slider.Value = _color.GetSaturation();
        sldL.Slider.Value = _color.GetBrightness();
        sldH.Slider.Value = _color.GetHue();

        brdColorDisplay.Background = new SolidColorBrush(_color);

        txbHexValue.Text = ColorUtils.ToHexStringWithoutAlpha(_color);

        _isSettingValues = false;

        ColorPicked?.Invoke(this, color);
    }

    public void LoadDefaultCustomPalette()
    {
        LoadCustomPalette(Path.Combine(ColorPickerSettings.CustomColorsDirectory, ColorPickerSettings.CustomColorsFilename));
    }

    public void ShowHueTab()
    {
        tbcTabs.SelectedIndex = 1;
    }

    internal void CustomColorsChanged()
    {
        if (ColorPickerSettings.UsingCustomPalette)
        {
            SaveCustomPalette(ColorPickerSettings.CustomPaletteFilename);
        }
    }

    // Internal

    const int NumColorsFirstSwatch = 39;
    const int NumColorsSecondSwatch = 112;

    static ColorPalette _colorPalette;

    readonly List<ColorSwatchItem> _colorSwatch1 = [];
    readonly List<ColorSwatchItem> _colorSwatch2 = [];

    Color _color = Colors.White;

    bool _isSettingValues = false;

    FormatConvertedBitmap _hueImage;
    byte[] _hueImageData;
    WriteableBitmap _hueImageBitmap;

    private void SetColorFromImageLocation(BitmapSource img, Point pos)
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

    private void SaveCustomPalette(string filename)
    {
        var colors = swtCustomColors.GetColors();
        _colorPalette.CustomColors = colors;

        try
        {
            _colorPalette.SaveToXml(filename);
        }
        catch { }
    }

    private void LoadCustomPalette(string filename)
    {
        if (File.Exists(filename))
        {
            try
            {
                _colorPalette = ColorPalette.LoadFromXml(filename);

                swtCustomColors.SwatchListBox.ItemsSource = _colorPalette.CustomColors.ToList();

                // Do regular one too

                _colorSwatch1.Clear();
                _colorSwatch2.Clear();
                _colorSwatch1.AddRange(_colorPalette.BuiltInColors.Take(NumColorsFirstSwatch).ToArray());
                _colorSwatch2.AddRange(_colorPalette.BuiltInColors.Skip(NumColorsFirstSwatch).Take(NumColorsSecondSwatch).ToArray());
                swtSwatch1.SwatchListBox.ItemsSource = _colorSwatch1;
                swtSwatch2.SwatchListBox.ItemsSource = _colorSwatch2;
            }
            catch { }
        }
    }

    private void LoadHueImage()
    {
        //Load the embedded resource
        var image = new BitmapImage(new Uri("pack://application:,,,/ColorPickerWPF;component/Resources/colorpicker2.png", UriKind.Absolute));

        _hueImage = new FormatConvertedBitmap();
        _hueImage.BeginInit();
        _hueImage.Source = image;
        _hueImage.DestinationFormat = PixelFormats.Pbgra32;
        _hueImage.EndInit();

        _hueImageData = new byte[_hueImage.PixelWidth * _hueImage.PixelHeight * 4];
        _hueImage.CopyPixels(_hueImageData, _hueImage.PixelWidth * 4, 0);

        _hueImageBitmap = new WriteableBitmap(_hueImage.PixelWidth, _hueImage.PixelHeight, 96, 96, PixelFormats.Pbgra32, null);

        imgHue.Source = _hueImageBitmap;
    }

    private void OnMouseMoveOverRainbow(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(imgRainbow);
        var img = imgRainbow.Source as BitmapSource;

        if (pos.X > 0 && pos.Y > 0 && pos.X < img.PixelWidth && pos.Y < img.PixelHeight)
            SetColorFromImageLocation(img, pos);
    }

    private void OnMouseMoveOverHue(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(imgHue);
        var img = imgHue.Source as BitmapSource;

        if (pos.X > 0 && pos.Y > 0 && pos.X < img.PixelWidth && pos.Y < img.PixelHeight)
            SetColorFromImageLocation(img, pos);
    }

    private void OnMouseUpOnRainbow(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        MouseMove -= OnMouseMoveOverRainbow;
        MouseUp -= OnMouseUpOnRainbow;
    }

    private void OnMouseUpOnHue(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        MouseMove -= OnMouseMoveOverHue;
        MouseUp -= OnMouseUpOnHue;
    }

    // UI

    private void HueTab_Selected(object sender, RoutedEventArgs e)
    {
        sldPickerHue.Value = _color.GetHue();
    }

    private void RainbowImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this);

        MouseMove += OnMouseMoveOverRainbow;
        MouseUp += OnMouseUpOnRainbow;
    }

    private void HueImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this);

        MouseMove += OnMouseMoveOverHue;
        MouseUp += OnMouseUpOnHue;
    }

    private void PickerHueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        //var hueChange = (int)((PickerHueSlider.Value / 360.0) * 240);
        var sliderHue = (float)sldPickerHue.Value;

        if (_hueImage == null)
        {
            LoadHueImage();
        }

        if (sliderHue <= 0f || sliderHue >= 360f)
        {
            // No hue change just return
            return;
        }

        var imagebuffer = new byte[_hueImageData.Length];

        // Copy the image data from our color picker into the array for our output image, so we dont change the values of the original image.
        Array.Copy(_hueImageData, imagebuffer, imagebuffer.Length);

        for (int x = 0; x < _hueImage.PixelWidth; x++)
        {
            for (int y = 0; y < _hueImage.PixelHeight; y++)
            {
                var offset = (y * _hueImage.PixelWidth + x) * 4;

                System.Drawing.Color pixel;

                pixel = System.Drawing.Color.FromArgb(255, imagebuffer[offset + 2],
                    imagebuffer[offset + 1], imagebuffer[offset]);

                var newHue = (float)(sliderHue + pixel.GetHue());
                if (newHue >= 360) newHue -= 360;

                var color = ColorUtils.FromAhsb((int)255, newHue, pixel.GetSaturation(), pixel.GetBrightness());

                imagebuffer[offset + 0] = color.B;
                imagebuffer[offset + 1] = color.G;
                imagebuffer[offset + 2] = color.R;
                imagebuffer[offset + 3] = color.A;
            }
        }

        // Copy back the changed pixels to the image
        _hueImageBitmap.Lock();
        Marshal.Copy(imagebuffer, 0, _hueImageBitmap.BackBuffer, imagebuffer.Length);
        _hueImageBitmap.AddDirtyRect(new Int32Rect(0, 0, _hueImageBitmap.PixelWidth, _hueImage.PixelHeight));
        _hueImageBitmap.Unlock();
    }

    private void RSlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var val = (byte)e.Value;
            _color.R = val;
            SetColor(_color);
        }
    }

    private void GSlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var val = (byte)e.Value;
            _color.G = val;
            SetColor(_color);
        }
    }

    private void BSlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var val = (byte)e.Value;
            _color.B = val;
            SetColor(_color);
        }
    }

    private void ASlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var val = (byte)e.Value;
            _color.A = val;
            SetColor(_color);
        }
    }

    private void HSlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var s = _color.GetSaturation();
            var l = _color.GetBrightness();
            var h = (float)e.Value;
            var a = (int)slrA.Slider.Value;
            _color = ColorUtils.FromAhsb(a, h, s, l);

            SetColor(_color);
        }
    }

    private void SSlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var s = (float)e.Value;
            var l = _color.GetBrightness();
            var h = _color.GetHue();
            var a = (int)slrA.Slider.Value;
            _color = ColorUtils.FromAhsb(a, h, s, l);

            SetColor(_color);
        }
    }

    private void LSlider_ValueChanged(object sender, SliderRow.ValueChangedEventArgs e)
    {
        if (!_isSettingValues)
        {
            var s = _color.GetSaturation();
            var l = (float)e.Value;
            var h = _color.GetHue();
            var a = (int)slrA.Slider.Value;
            _color = ColorUtils.FromAhsb(a, h, s, l);

            SetColor(_color);
        }
    }

    private void HexValue_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_isSettingValues && IsLoaded)
        {
            SetColor(ColorUtils.FromHexString(txbHexValue.Text));
        }
    }

    private void Swatch_ColorPicked(object sender, ColorPickerSwatch.ColorPickedEventArgs e)
    {
        SetColor(e.Color);
    }
}