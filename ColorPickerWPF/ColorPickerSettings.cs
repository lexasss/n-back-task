using System;
using System.IO;

namespace ColorPickerWPF;

public static class ColorPickerSettings
{
    public static string CustomColorsFilename { get; set; } = "CustomColorPalette.xml";
    public static string CustomColorsDirectory { get; set; } = Environment.CurrentDirectory;

    public static string CustomPaletteFilename => Path.Combine(CustomColorsDirectory, CustomColorsFilename);

    internal static bool UsingCustomPalette { get; set; } = false;
}