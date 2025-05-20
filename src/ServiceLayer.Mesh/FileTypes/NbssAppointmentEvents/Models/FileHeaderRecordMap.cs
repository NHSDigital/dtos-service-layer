using CsvHelper.Configuration;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public sealed class FileHeaderRecordMap : ClassMap<FileHeaderRecord>
{
    public FileHeaderRecordMap()
    {
        Map(m => m.RecordTypeIdentifier).Index(0);
        Map(m => m.ExtractId).Index(1);
        Map(m => m.TransferStartDate).Index(2);
        Map(m => m.TransferStartTime).Index(3);
        Map(m => m.RecordCount).Index(4);
    }
}
