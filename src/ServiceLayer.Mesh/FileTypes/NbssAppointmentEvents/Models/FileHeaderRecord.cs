namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public class FileHeaderRecord
{
    public string? RecordTypeIdentifier { get; set; }

    public string? ExtractId { get; set; }

    public string? TransferStartDate { get; set; }

    public string? TransferStartTime { get; set; }

    public string? RecordCount { get; set; }
}
