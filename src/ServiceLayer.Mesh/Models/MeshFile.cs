using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ServiceLayer.Mesh.Models;

public class MeshFile
{
    public required string FileId { get; set; }
    public required MeshFileType FileType { get; set; }
    public required string MailboxId { get; set; }
    public required MeshFileStatus Status { get; set; }
    public string? BlobPath { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
