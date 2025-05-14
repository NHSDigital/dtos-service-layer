namespace ServiceLayer.Mesh.Configuration;

public interface IFileRetryFunctionConfiguration
{
    string StaleHours { get; }
}
