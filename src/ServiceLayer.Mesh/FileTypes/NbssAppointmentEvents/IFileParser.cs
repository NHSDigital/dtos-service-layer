using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public interface IFileParser
{
    ParsedFile Parse(Stream stream);
}
