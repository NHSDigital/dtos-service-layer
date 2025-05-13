using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Storage;

public class MeshFilesBlobStore : IMeshFilesBlobStore
{
    public Task<Stream> DownloadAsync(MeshFile file)
    {
        throw new NotImplementedException();
    }

    public Task UploadAsync(MeshFile file, byte[] data)
    {
        throw new NotImplementedException();
    }
}
