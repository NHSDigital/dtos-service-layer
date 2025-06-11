namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class FileParsingException : Exception
{
    public string Code { get; }

    public FileParsingException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public FileParsingException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

