using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLayer.Data.Models;

public class NbssAppointmentEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    [StringLength(255)]
    public required string MeshFileId { get; set; }
    [StringLength(3, MinimumLength = 3)]
    [Column(TypeName = "char(3)")]
    public required string BSO { get; set; }
    [StringLength(8, MinimumLength = 8)]
    [Column(TypeName = "char(8)")]
    public required string ExtractId { get; set; }
    [StringLength(6, MinimumLength = 6)]
    [Column(TypeName = "char(6)")]
    public required string Sequence { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public required string Action { get; set; }
    [StringLength(5)]
    [Column(TypeName = "varchar(5)")]
    public required string ClinicCode { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public string? HoldingClinic { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public required string Status { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public string? AttendedNotScreened { get; set; }
    [StringLength(27)]
    [Column(TypeName = "varchar(27)")]
    public required string AppointmenId { get; set; }
    [StringLength(10, MinimumLength = 10)]
    [Column(TypeName = "char(10)")]
    public required string NhsNumber { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public required string EpisodeType { get; set; }
    public required DateOnly EpisodeStart { get; set; }
    [StringLength(9)]
    [Column(TypeName = "varchar(9)")]
    public required string BatchId { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public required string AppointmentType { get; set; }
    public byte? ScreeningAppointmentNumber { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public required string BookedBy { get; set; }
    [StringLength(1, MinimumLength = 1)]
    [Column(TypeName = "char(1)")]
    public string? CancelledBy { get; set; }
    [Column(TypeName = "datetime2(0)")]
    public required DateTime AppointmentDateTime { get; set; }
    [StringLength(5)]
    [Column(TypeName = "varchar(5)")]
    public required string Location { get; set; }
    [StringLength(40)]
    [Column(TypeName = "varchar(40)")]
    public required string ClinicName { get; set; }
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public required string ClinicNameOnLetters { get; set; }
    [StringLength(30)]
    [Column(TypeName = "varchar(30)")]
    public required string ClinicAddressLine1 { get; set; }
    [StringLength(30)]
    [Column(TypeName = "varchar(30)")]
    public required string ClinicAddressLine2 { get; set; }
    [StringLength(30)]
    [Column(TypeName = "varchar(30)")]
    public required string ClinicAddressLine3 { get; set; }
    [StringLength(30)]
    [Column(TypeName = "varchar(30)")]
    public required string ClinicAddressLine4 { get; set; }
    [StringLength(30)]
    [Column(TypeName = "varchar(30)")]
    public required string ClinicAddressLine5 { get; set; }
    [StringLength(8)]
    [Column(TypeName = "varchar(8)")]
    public required string ClinicPostcode { get; set; }
    [Column(TypeName = "datetime2(0)")]
    public required DateTime ActionTimestamp { get; set; }
}
