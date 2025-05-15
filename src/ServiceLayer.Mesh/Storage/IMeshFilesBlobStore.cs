using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Storage;

public interface IMeshFilesBlobStore
{
    // TODO - return a Stream or a byte array?
    public Task<Stream> DownloadAsync(MeshFile file);

    // Mesh client gives us a byte array, hence this not taking a stream.
    public Task<string> UploadAsync(MeshFile file, byte[] data);
}
