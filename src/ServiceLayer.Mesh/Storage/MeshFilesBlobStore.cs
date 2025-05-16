using Azure.Storage.Blobs;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Storage;

public class MeshFilesBlobStore(BlobContainerClient blobContainerClient) : IMeshFilesBlobStore
{
    public async Task<Stream> DownloadAsync(MeshFile file)
    {
        var blobClient = blobContainerClient.GetBlobClient(file.BlobPath);
        return (await blobClient.DownloadAsync()).Value.Content;
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
