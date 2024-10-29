using System.Globalization;
using System.Windows.Controls;

namespace NBackTask;

public class RangeRule : ValidationRule
{
    public int Min { get; set; }
    public int Max { get; set; }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        int valueNum = 0;
        string valueStr = (string)value;

        try
        {
            if (valueStr.Length > 0)
                valueNum = int.Parse(valueStr);
        }
        catch (Exception e)
        {
            return new ValidationResult(false, $"Illegal characters or {e.Message}");
        }

        if ((valueNum < Min) || (valueNum > Max))
        {
            return new ValidationResult(false,
              $"Please enter a number in the range: {Min}-{Max}.");
        }
        return ValidationResult.ValidResult;
    }
}

public class NonBlankRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var result = ((string)value).Length > 0;
        if (!result)
        {
            return new ValidationResult(false, $"The text cannot be blank");
        }

        return ValidationResult.ValidResult;
    }
}