namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

public class FileDataRecord
{
    public int RowNumber { get; set; }
    public Dictionary<string, string> Fields { get; } = [];

    public string? this[string fieldName] => Fields.GetValueOrDefault(fieldName);
}
