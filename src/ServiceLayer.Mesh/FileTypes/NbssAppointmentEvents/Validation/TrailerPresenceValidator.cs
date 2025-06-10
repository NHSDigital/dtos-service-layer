using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public class TrailerPresenceValidator : IFileValidator
{
    public IEnumerable<ValidationError> Validate(ParsedFile file)
    {
        if (file.FileTrailer == null)
        {
            yield return new ValidationError
            {
                Code = ErrorCodes.MissingTrailer,
                Error = "Trailer is missing",
                Scope = ValidationErrorScope.File
            };
        }
    }
}
