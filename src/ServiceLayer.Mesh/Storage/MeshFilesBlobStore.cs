using Azure.Storage.Blobs;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Storage;

public class MeshFilesBlobStore(BlobContainerClient blobContainerClient) : IMeshFilesBlobStore
{
    public Task<Stream> DownloadAsync(MeshFile file)
    {
        throw new NotImplementedException();
    }

    public async Task<string> UploadAsync(MeshFile file, byte[] data)
    {
        var blobPath = $"{file.FileType}/{file.FileId}";
        var blobClient = blobContainerClient.GetBlobClient(blobPath);

        var dataStream = new MemoryStream(data);

        await blobClient.UploadAsync(dataStream, overwrite: true);

        return blobPath;
    }
}
