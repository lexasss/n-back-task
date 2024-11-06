using System;

namespace ColorPickerWPF;

[Flags]
public enum DialogOptions
{
    None = 0,
    SimpleView = 1,
    LoadCustomPalette = 2,
    HuePicker = 4,
}