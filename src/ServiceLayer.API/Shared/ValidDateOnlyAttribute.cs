using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.API.Shared;

public class ValidDateOnlyAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string s && DateOnly.TryParse(s, out _))
        {
            return true;
        }

        return false;
    }
}
