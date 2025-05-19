namespace ServiceLayer.Mesh.Configuration;

public interface IFileRetryFunctionConfiguration
{
    int StaleHours { get; }
}
