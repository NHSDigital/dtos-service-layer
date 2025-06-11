using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public interface IRecordValidator
{
    IEnumerable<ValidationError> Validate(FileDataRecord fileDataRecord);
}
