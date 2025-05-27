using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh;

public interface IFileTransformer
{
    bool CanHandle(MeshFileType fileType);
    Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData);
}
