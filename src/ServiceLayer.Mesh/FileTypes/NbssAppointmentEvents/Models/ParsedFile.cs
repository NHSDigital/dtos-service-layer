namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public class ParsedFile
{
    public FileHeaderRecord? FileHeader { get; set; }
    public FileTrailerRecord? FileTrailer { get; set; }
    public required List<string> ColumnHeadings { get; set; } = [];
    public required List<FileDataRecord> DataRecords { get; set; } = [];
}
