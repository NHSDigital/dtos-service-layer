using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLayer.Data.Models;

[Table("MeshFileEvents")]
public class MeshFileEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();

    [MaxLength(255)]
    public required string FileId { get; set; }

    public required DateTime TimestampUtc { get; set; }

    [MaxLength(20)]
    public required MeshFileStatus Status { get; set; }

    [MaxLength(20)]
    public required FileEventSource Source { get; set; }
}
