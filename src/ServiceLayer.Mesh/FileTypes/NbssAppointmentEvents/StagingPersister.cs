using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

// TODO - class to take validated AppointmentEventsFile and save the records to NbssAppointmentEvents table
public class StagingPersister : IStagingPersister
{
    public Task WriteStagedData(ParsedFile parsedFile)
    {
        // TODO - implement this
        throw new NotImplementedException();
    }
}
