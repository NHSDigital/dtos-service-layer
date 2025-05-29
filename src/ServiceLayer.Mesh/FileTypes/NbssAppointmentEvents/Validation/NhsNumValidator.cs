using System.Text.RegularExpressions;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public partial class NhsNumValidator() :
    RegexValidator("NHS Num", NhsNumberRegex(), ErrorCodes.MissingNhsNum, ErrorCodes.InvalidNhsNum)
{
    protected override IEnumerable<ValidationError> RunAdditionalChecks(int rowNumber, string value)
    {
        if (!HasValidCheckDigit(value))
        {
            yield return new ValidationError
            {
                Scope = ValidationErrorScope.Record,
                RowNumber = rowNumber,
                Field = FieldName,
                Error = "NHS Num has invalid check digit",
                Code = ErrorCodes.InvalidNhsNumCheckDigit
            };
        }
    }

    private static bool HasValidCheckDigit(string value)
    {
        var weightedSum = 0;
        for (var i = 0; i < 9; i++)
        {
            weightedSum += (10 - i) * (value[i] - '0');
        }

        var remainder = weightedSum % 11;
        var expectedCheckDigit = (11 - remainder) % 11;

        return expectedCheckDigit == value[9] - '0';
    }

    [GeneratedRegex(@"^\d{10}$", RegexOptions.Compiled)]
    private static partial Regex NhsNumberRegex();
}
