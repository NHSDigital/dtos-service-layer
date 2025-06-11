using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class HeaderPresenceValidator : IFileValidator
{
    public IEnumerable<ValidationError> Validate(ParsedFile file)
    {
        if (file.FileHeader == null)
        {
            yield return new ValidationError
            {
                Code = ErrorCodes.MissingHeader,
                Error = "Header is missing",
                Scope = ValidationErrorScope.File
            };
        }
    }
}
