using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh;

public abstract class FileTransformerBase : IFileTransformer
{
    protected abstract MeshFileType HandlesFileType { get; }
    public virtual bool CanHandle(MeshFileType fileType) => fileType == HandlesFileType;
    public abstract Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData);
}
