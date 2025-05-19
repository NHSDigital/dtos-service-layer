using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

// TODO - interface for class to take validated AppointmentEventsFile and save the records to NbssAppointmentEvents table
public interface IStagingPersister
{
    Task WriteStagedData(ParsedFile parsedFile);
}
