using Azure.Storage.Blobs;
using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh.Storage;

public class MeshFilesBlobStore : IMeshFilesBlobStore
{
    private readonly BlobContainerClient _blobContainerClient;

    public MeshFilesBlobStore(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient;
        EnsureContainerExists();
    }

    public async Task<Stream> DownloadAsync(MeshFile file)
    {
        var blobClient = _blobContainerClient.GetBlobClient(file.BlobPath);
        return (await blobClient.DownloadAsync()).Value.Content;
    }

    public async Task<string> UploadAsync(MeshFile file, byte[] data)
    {
        var blobPath = $"{file.FileType}/{file.FileId}";
        var blobClient = _blobContainerClient.GetBlobClient(blobPath);

        var dataStream = new MemoryStream(data);

        await blobClient.UploadAsync(dataStream, overwrite: true);

        return blobPath;
    }

    private void EnsureContainerExists()
    {
        _blobContainerClient.CreateIfNotExists();
    }
}
