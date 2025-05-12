using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ServiceLayer.Mesh.Models;

public class MeshFile
{
    [MaxLength(255)]
    public required string FileId { get; set; }
    [MaxLength(50)]
    public required MeshFileType FileType { get; set; }
    [MaxLength(50)]
    public required string MailboxId { get; set; }
    [MaxLength(20)]
    public required MeshFileStatus Status { get; set; }
    [MaxLength(1024)]
    public string? BlobPath { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
