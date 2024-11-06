using System;
using Color = System.Windows.Media.Color;

namespace ColorPickerWPF;

internal static class ColorUtils
{
    public static string ToHexString(this Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    
    public static string ToHexStringWithoutAlpha(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static Color FromHexString(string hex)
    {
        return Color.FromRgb(
           Convert.ToByte(hex.Substring(1, 2), 16),
           Convert.ToByte(hex.Substring(3, 2), 16),
           Convert.ToByte(hex.Substring(5, 2), 16));
    }

    public static float GetHue(this Color c) => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B).GetHue();

    public static float GetBrightness(this Color c) => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B).GetBrightness();

    public static float GetSaturation(this Color c) => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B).GetSaturation();

    public static Color FromAhsb(int alpha, float hue, float saturation, float brightness)
    {
        if (0 > alpha || 255 < alpha)
        {
            throw new ArgumentOutOfRangeException(nameof(alpha), alpha,
              "Value must be within a range of 0 - 255.");
        }
        if (0f > hue || 360f < hue)
        {
            throw new ArgumentOutOfRangeException(nameof(hue), hue,
              "Value must be within a range of 0 - 360.");
        }
        if (0f > saturation || 1f < saturation)
        {
            throw new ArgumentOutOfRangeException(nameof(saturation), saturation,
              "Value must be within a range of 0 - 1.");
        }
        if (0f > brightness || 1f < brightness)
        {
            throw new ArgumentOutOfRangeException(nameof(brightness), brightness,
              "Value must be within a range of 0 - 1.");
        }

        if (0 == saturation)
        {
            return Color.FromArgb((byte)alpha, Convert.ToByte(brightness * 255),
              Convert.ToByte(brightness * 255), Convert.ToByte(brightness * 255));
        }

        float fMax, fMid, fMin;
        byte iMax, iMid, iMin;
        int sextant;

        if (0.5 < brightness)
        {
            fMax = brightness - (brightness * saturation) + saturation;
            fMin = brightness + (brightness * saturation) - saturation;
        }
        else
        {
            fMax = brightness + (brightness * saturation);
            fMin = brightness - (brightness * saturation);
        }

        sextant = (int)Math.Floor(hue / 60f);
        if (300f <= hue)
        {
            hue -= 360f;
        }
        hue /= 60f;
        hue -= 2f * (float)Math.Floor(((sextant + 1f) % 6f) / 2f);
        if (0 == sextant % 2)
        {
            fMid = hue * (fMax - fMin) + fMin;
        }
        else
        {
            fMid = fMin - hue * (fMax - fMin);
        }

        iMax = Convert.ToByte(fMax * 255);
        iMid = Convert.ToByte(fMid * 255);
        iMin = Convert.ToByte(fMin * 255);

        return sextant switch
        {
            1 => Color.FromArgb((byte)alpha, iMid, iMax, iMin),
            2 => Color.FromArgb((byte)alpha, iMin, iMax, iMid),
            3 => Color.FromArgb((byte)alpha, iMin, iMid, iMax),
            4 => Color.FromArgb((byte)alpha, iMid, iMin, iMax),
            5 => Color.FromArgb((byte)alpha, iMax, iMin, iMid),
            _ => Color.FromArgb((byte)alpha, iMax, iMid, iMin)
        };
    }
}