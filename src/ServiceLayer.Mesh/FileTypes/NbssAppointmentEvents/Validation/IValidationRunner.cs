using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public interface IValidationRunner
{
    IList<ValidationError> Validate(ParsedFile file);
}


