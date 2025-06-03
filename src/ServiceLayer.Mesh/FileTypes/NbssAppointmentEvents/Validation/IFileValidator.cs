using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public interface IFileValidator
{
    IEnumerable<ValidationError> Validate(ParsedFile file);
}
