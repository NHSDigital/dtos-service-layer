using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class ValidationRunner(
    IValidationRunnerConfiguration configuration,
    IEnumerable<IFileValidator> fileValidators,
    IEnumerable<IRecordValidator> recordValidators)
    : IValidationRunner
{
    public IList<ValidationError> Validate(ParsedFile file)
    {
        var errors = new List<ValidationError>();

        RunFileValidators(file, errors);
        if (errors.Count >= configuration.MaximumValidationErrors)
        {
            return FinalizeEarly(errors);
        }

        RunRecordValidators(file, errors);
        if (errors.Count >= configuration.MaximumValidationErrors)
        {
            return FinalizeEarly(errors);
        }

        return errors;
    }

    private void RunFileValidators(ParsedFile file, List<ValidationError> errors)
    {
        foreach (var validator in fileValidators)
        {
            var results = validator.Validate(file);
            AddErrorsWithCap(results, errors);
            if (errors.Count >= configuration.MaximumValidationErrors) return;
        }
    }

    private void RunRecordValidators(ParsedFile file, List<ValidationError> errors)
    {
        foreach (var record in file.DataRecords)
        {
            foreach (var validator in recordValidators)
            {
                var results = validator.Validate(record);
                AddErrorsWithCap(results, errors);
                if (errors.Count >= configuration.MaximumValidationErrors) return;
            }
        }
    }

    private void AddErrorsWithCap(IEnumerable<ValidationError> newErrors, List<ValidationError> existingErrors)
    {
        foreach (var error in newErrors)
        {
            if (existingErrors.Count >= configuration.MaximumValidationErrors) break;
            existingErrors.Add(error);
        }
    }

    private List<ValidationError> FinalizeEarly(List<ValidationError> errors)
    {
        errors.Add(new ValidationError
        {
            Code = ErrorCodes.ValidationAborted,
            Error = $"Validation aborted after {configuration.MaximumValidationErrors} errors encountered",
            Scope = ValidationErrorScope.File
        });

        return errors;
    }
}
