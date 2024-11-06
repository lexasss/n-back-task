using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace ColorPickerWPF;

[Serializable]
public class ColorPalette
{
    public ColorSwatchItem[] BuiltInColors { get; set; } = [];

    public ColorSwatchItem[] CustomColors { get; set; } = [];

    public void InitializeDefaults()
    {
        BuiltInColors = GetColorSwatchItems([
            Colors.Black,
            Colors.Red,
            Colors.DarkOrange,
            Colors.Yellow,
            Colors.LawnGreen,
            Colors.Blue,
            Colors.Purple,
            Colors.DeepPink,
            Colors.Aqua,
            Colors.SaddleBrown,
            Colors.Wheat,
            Colors.BurlyWood,
            Colors.Teal,

            Colors.White,
            Colors.OrangeRed,
            Colors.Orange,
            Colors.Gold,
            Colors.LimeGreen,
            Colors.DodgerBlue,
            Colors.Orchid,
            Colors.HotPink,
            Colors.Turquoise,
            Colors.SandyBrown,
            Colors.SeaGreen,
            Colors.SlateBlue,
            Colors.RoyalBlue,

            Colors.Tan,
            Colors.Peru,
            Colors.DarkBlue,
            Colors.DarkGreen,
            Colors.DarkSlateBlue,
            Colors.Navy,
            Colors.MistyRose,
            Colors.LemonChiffon,
            Colors.ForestGreen,
            Colors.Firebrick,
            Colors.DarkViolet,
            Colors.Aquamarine,
            Colors.CornflowerBlue,

            Colors.Bisque,
            Colors.WhiteSmoke,
            Colors.AliceBlue,

            Color.FromArgb(255, 5, 5, 5),
            Color.FromArgb(255, 15, 15, 15),
            Color.FromArgb(255, 35, 35, 35),
            Color.FromArgb(255, 55, 55, 55),
            Color.FromArgb(255, 75, 75, 75),
            Color.FromArgb(255, 95, 95, 95),
            Color.FromArgb(255, 115, 115, 115),
            Color.FromArgb(255, 135, 135, 135),
            Color.FromArgb(255, 155, 155, 155),
            Color.FromArgb(255, 175, 175, 175),
            Color.FromArgb(255, 195, 195, 195),
            Color.FromArgb(255, 215, 215, 215),
            Color.FromArgb(255, 235, 235, 235),
        ]).ToArray();

        CustomColors = Enumerable.Repeat(Colors.White, NumColorsCustomSwatch)
            .Select(x => new ColorSwatchItem() { Color = x, HexString = x.ToHexString() })
            .ToArray();
    }

    public static ColorPalette LoadFromXml(string filename)
    {
        ColorPalette result = default;
        if (File.Exists(filename))
        {
            var sr = new StreamReader(filename);
            var xr = new XmlTextReader(sr);

            var xmlSerializer = new XmlSerializer(typeof(ColorPalette));

            result = (ColorPalette)xmlSerializer.Deserialize(xr);

            xr.Close();
            sr.Close();
            xr.Dispose();
            sr.Dispose();
        }
        return result;
    }

    public void SaveToXml(string filename)
    {
        var xmlSerializer = new XmlSerializer(typeof(ColorPalette));

        var sww = new StringWriter();

        var settings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "    ",
            NewLineOnAttributes = false,
            //OmitXmlDeclaration = true
        };
        var writer = XmlWriter.Create(sww, settings);

        xmlSerializer.Serialize(writer, this);
        var xml = sww.ToString();

        writer.Close();
        writer.Dispose();

        File.WriteAllText(filename, xml);
    }

    // Internal

    [XmlIgnore]
    protected const int NumColorsFirstSwatch = 3 * 13;

    [XmlIgnore]
    protected const int NumColorsSecondSwatch = 3 + 13;

    [XmlIgnore]
    protected const int NumColorsCustomSwatch = 22;

    private IEnumerable<ColorSwatchItem> GetColorSwatchItems(Color[] colors) =>
        colors.Select(x => new ColorSwatchItem() { Color = x, HexString = x.ToHexString() });
}