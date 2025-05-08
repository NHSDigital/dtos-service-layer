namespace ServiceLayer.Mesh.Models;

public enum MeshFileStatus
{
    Discovered,
    Extracting,
    Extracted,
    Transforming,
    Transformed,
    FailedExtract,
    FailedTransform
}
