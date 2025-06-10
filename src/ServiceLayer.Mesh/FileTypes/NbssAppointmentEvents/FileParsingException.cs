namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class FileParsingException : Exception
{
    public string Code { get; }
    public string ErrorMessage { get; }

    public FileParsingException(string code, string errorMessage)
        : base(errorMessage)
    {
        Code = code;
        ErrorMessage = errorMessage;
    }

    public FileParsingException(string code, string errorMessage, Exception innerException)
        : base(errorMessage, innerException)
    {
        Code = code;
        ErrorMessage = errorMessage;
    }
}
