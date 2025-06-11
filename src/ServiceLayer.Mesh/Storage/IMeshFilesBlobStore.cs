using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh.Storage;

public interface IMeshFilesBlobStore
{
    public Task<Stream> DownloadAsync(MeshFile file);

    // Mesh client gives us a byte array, hence this is not taking a stream.
    public Task<string> UploadAsync(MeshFile file, byte[] data);
}
