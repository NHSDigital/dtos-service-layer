using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class ValidationRunner(
    IEnumerable<IFileValidator> fileValidators,
    IEnumerable<IRecordValidator> recordValidators)
    : IValidationRunner
{
    public IList<ValidationError> Validate(ParsedFile file)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in fileValidators)
        {
            errors.AddRange(validator.Validate(file));
        }

        foreach (var dataRecord in file.DataRecords)
        {
            foreach (var recordValidator in recordValidators)
            {
                errors.AddRange(recordValidator.Validate(dataRecord));
            }
        }

        return errors;
    }
}
