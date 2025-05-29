using System.Globalization;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class DateFormatValidator(
    string fieldName,
    string format,
    string errorCodeMissing,
    string errorCodeInvalidFormat) : IRecordValidator
{
    public IEnumerable<ValidationError> Validate(FileDataRecord fileDataRecord)
    {
        var value = fileDataRecord[fieldName];

        if (value == null)
        {
            yield return new ValidationError
            {
                Scope = ValidationErrorScope.Record,
                RowNumber = fileDataRecord.RowNumber,
                Field = fieldName,
                Error = $"{fieldName} is missing",
                Code = errorCodeMissing,
            };
            yield break;
        }

        if (!DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            yield return new ValidationError
            {
                Scope = ValidationErrorScope.Record,
                RowNumber = fileDataRecord.RowNumber,
                Field = fieldName,
                Error = $"{fieldName} is in an invalid format",
                Code = errorCodeInvalidFormat,
            };
        }
    }
}
