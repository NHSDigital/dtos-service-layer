using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ServiceLayer.API.Shared;

[AttributeUsage(AttributeTargets.Property)]
public class ValidDateOnlyAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string s && DateOnly.TryParse(s, CultureInfo.CurrentCulture, out _))
        {
            return true;
        }

        return false;
    }
}
