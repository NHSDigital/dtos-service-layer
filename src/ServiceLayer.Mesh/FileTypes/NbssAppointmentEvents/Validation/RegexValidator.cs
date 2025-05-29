using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class RegexValidator(
    string fieldName,
    Regex pattern,
    string errorCodeMissing,
    string errorCodeInvalidFormat)
    : IRecordValidator
{
    protected string FieldName { get; } = fieldName;

    public IEnumerable<ValidationError> Validate(FileDataRecord fileDataRecord)
    {
        var value = fileDataRecord[FieldName];

        if (value == null)
        {
            yield return new ValidationError
            {
                RowNumber = fileDataRecord.RowNumber,
                Field = FieldName,
                Error = $"{FieldName} is missing",
                Code = errorCodeMissing,
            };
            yield break;
        }

        if (!pattern.IsMatch(value))
        {
            yield return new ValidationError
            {
                RowNumber = fileDataRecord.RowNumber,
                Field = FieldName,
                Error = $"{FieldName} is in an invalid format",
                Code = errorCodeInvalidFormat,
            };
            yield break;
        }

        foreach (var additionalError in RunAdditionalChecks(fileDataRecord.RowNumber, value))
        {
            yield return additionalError;
        }
    }

    protected virtual IEnumerable<ValidationError> RunAdditionalChecks(int rowNumber, string value)
    {
        yield break;
    }
}
