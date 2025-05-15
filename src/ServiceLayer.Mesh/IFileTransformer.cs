using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh;

public interface IFileTransformer
{
    MeshFileType HandlesFileType { get; }
    Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData);
}
