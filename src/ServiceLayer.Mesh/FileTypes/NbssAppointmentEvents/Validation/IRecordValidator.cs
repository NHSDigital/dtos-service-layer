using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

// TODO - create a whole bunch of implementations of this to perform the validation against NBSS Appointment events records
public interface IRecordValidator
{
    IEnumerable<ValidationError> Validate(FileDataRecord fileDataRecord);
}
