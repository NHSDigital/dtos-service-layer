using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Storage;

public interface IMeshFilesBlobStore
{
    public Task<Stream> DownloadAsync(MeshFile file);
    public Task UploadAsync(MeshFile file, byte[] data);
}
