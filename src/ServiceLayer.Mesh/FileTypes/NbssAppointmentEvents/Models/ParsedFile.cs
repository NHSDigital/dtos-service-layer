namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public class ParsedFile
{
    public FileHeaderRecord? FileHeader { get; set; }
    public FileTrailerRecord? FileTrailer { get; set; }
    public List<FileDataRecord> DataRecords { get; set; } = [];
}
