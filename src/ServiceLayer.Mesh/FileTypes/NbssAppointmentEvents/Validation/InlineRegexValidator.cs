using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class InlineRegexValidator(
    string fieldName,
    Regex pattern,
    string errorCodeMissing,
    string errorCodeInvalidFormat)
    : IRecordValidator
{
    public IEnumerable<ValidationError> Validate(FileDataRecord fileDataRecord)
    {
        var value = fileDataRecord[fieldName];

        if (value == null)
        {
            yield return new ValidationError
            {
                RowNumber = fileDataRecord.RowNumber,
                Field = fieldName,
                Error = $"{fieldName} is missing",
                Code = errorCodeMissing,
            };
            yield break;
        }

        if (!pattern.IsMatch(value))
        {
            yield return new ValidationError
            {
                RowNumber = fileDataRecord.RowNumber,
                Field = fieldName,
                Error = $"{fieldName} is in an invalid format",
                Code = errorCodeInvalidFormat,
            };
        }
    }
}
