namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public class ParsedFile
{
    public FileControlRecord? FileHeader { get; set; }
    public FileControlRecord? FileTrailer { get; set; }
    public required List<string> ColumnHeadings { get; set; } = [];
    public required List<FileDataRecord> DataRecords { get; set; } = [];
}
