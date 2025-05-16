using Azure.Storage.Blobs;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Storage;

public class MeshFilesBlobStore(BlobContainerClient blobContainerClient) : IMeshFilesBlobStore
{
    public async Task<Stream> DownloadAsync(MeshFile file)
    {
        var blobClient = blobContainerClient.GetBlobClient(file.BlobPath);

        var dataStream = new MemoryStream();

        await blobClient.DownloadToAsync(dataStream);

        dataStream.Close();

        return dataStream;
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
