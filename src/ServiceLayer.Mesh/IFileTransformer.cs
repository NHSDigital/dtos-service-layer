using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh;

public interface IFileTransformer
{
    bool CanHandle(MeshFileType fileType);
    Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData);
}

public abstract class FileTransformerBase : IFileTransformer
{
    protected abstract MeshFileType HandlesFileType { get; }
    public virtual bool CanHandle(MeshFileType fileType) => fileType == HandlesFileType;
    public abstract Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData);
}
