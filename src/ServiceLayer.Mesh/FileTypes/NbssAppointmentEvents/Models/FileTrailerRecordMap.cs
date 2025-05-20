using CsvHelper.Configuration;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public sealed class FileTrailerRecordMap : ClassMap<FileTrailerRecord>
{
    public FileTrailerRecordMap()
    {
        Map(m => m.RecordTypeIdentifier).Index(0);
        Map(m => m.ExtractId).Index(1);
        Map(m => m.TransferEndDate).Index(2);
        Map(m => m.TransferEndTime).Index(3);
        Map(m => m.RecordCount).Index(4);
    }
}
