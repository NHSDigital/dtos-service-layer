using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class InlineMaxLengthValidator(string fieldName, int maxLength, string errorCodeMissing, string errorCodeTooLong, bool allowEmpty = false)
    : IRecordValidator
{
    public IEnumerable<ValidationError> Validate(FileDataRecord fileDataRecord)
    {
        var value = fileDataRecord[fieldName];

        if (value == null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
        {
            var error = $"{fieldName} is missing{(allowEmpty ? "" : " or empty")}";
            
            yield return new ValidationError
            {
                RowNumber = fileDataRecord.RowNumber,
                Field = fieldName,
                Error = error,
                Code = errorCodeMissing,
            };
            yield break;
        }

        if (value.Length > maxLength)
        {
            var error = $"{fieldName} exceeds maximum length of {maxLength}";

            yield return new ValidationError
            {
                RowNumber = fileDataRecord.RowNumber,
                Field = fieldName,
                Error = error,
                Code = errorCodeTooLong,
            };
        }
    }
}
