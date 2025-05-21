using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public interface IStagingPersister
{
    Task WriteStagedData(ParsedFile parsedFile, MeshFile meshFile);
}
