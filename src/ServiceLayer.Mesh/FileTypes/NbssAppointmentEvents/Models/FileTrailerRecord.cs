namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public class FileTrailerRecord
{
    public string? RecordTypeIdentifier { get; set; }

    public string? ExtractId { get; set; }

    public string? TransferEndDate { get; set; }

    public string? TransferEndTime { get; set; }

    public string? RecordCount { get; set; }
}
