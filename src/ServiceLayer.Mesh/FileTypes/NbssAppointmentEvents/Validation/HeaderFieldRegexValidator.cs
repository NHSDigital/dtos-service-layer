using System.Linq.Expressions;
using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class HeaderFieldRegexValidator(
    Expression<Func<FileHeaderRecord, string?>> fieldSelector,
    string fieldName,
    Regex pattern,
    string errorCodeMissing,
    string errorCodeInvalidFormat)
    : IFileValidator
{
    public IEnumerable<ValidationError> Validate(ParsedFile file)
    {
        if (file.FileHeader == null)
        {
            yield break;
        }

        var hasErrored = false;
        var header = file.FileHeader!;
        var value = fieldSelector.Compile().Invoke(header);

        if (value == null)
        {
            yield return new ValidationError
            {
                Scope = ValidationErrorScope.Header,
                Field = fieldName,
                Error = $"{fieldName} is missing",
                Code = errorCodeMissing,
            };
            yield break;
        }

        if (!pattern.IsMatch(value))
        {
            hasErrored = true;
            yield return new ValidationError
            {
                Scope = ValidationErrorScope.Header,
                Field = fieldName,
                Error = $"{fieldName} is in an invalid format",
                Code = errorCodeInvalidFormat,
            };
        }

        foreach (var additionalError in RunAdditionalChecks(file, value, hasErrored))
        {
            yield return additionalError;
        }
    }

    protected virtual IEnumerable<ValidationError> RunAdditionalChecks(ParsedFile file, string value, bool hasErrored)
    {
        yield break;
    }
}
