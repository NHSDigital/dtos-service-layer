using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLayer.Mesh.Migrations
{
    /// <inheritdoc />
    public partial class AddNbssAppointmentEventTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NbssAppointmentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeshFileId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BSO = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    ExtractId = table.Column<string>(type: "char(8)", maxLength: 8, nullable: false),
                    Sequence = table.Column<string>(type: "char(6)", maxLength: 6, nullable: false),
                    Action = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    ClinicCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    HoldingClinic = table.Column<string>(type: "char(1)", maxLength: 1, nullable: true),
                    Status = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    AttendedNotScreened = table.Column<string>(type: "char(1)", maxLength: 1, nullable: true),
                    AppointmenId = table.Column<string>(type: "nvarchar(27)", maxLength: 27, nullable: false),
                    NhsNumber = table.Column<string>(type: "char(10)", maxLength: 10, nullable: false),
                    EpisodeType = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    EpisodeStart = table.Column<DateOnly>(type: "date", nullable: false),
                    BatchId = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    AppointmentType = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    ScreeningAppointmentNumber = table.Column<byte>(type: "tinyint", nullable: true),
                    BookedBy = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    CancelledBy = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    AppointmentDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ClinicName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClinicNameOnLetters = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClinicAddressLine1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClinicAddressLine2 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClinicAddressLine3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClinicAddressLine4 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClinicAddressLine5 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClinicPostcode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ActionTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NbssAppointmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NbssAppointmentEvents_MeshFiles_MeshFileId",
                        column: x => x.MeshFileId,
                        principalTable: "MeshFiles",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NbssAppointmentEvents_MeshFileId",
                table: "NbssAppointmentEvents",
                column: "MeshFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NbssAppointmentEvents");
        }
    }
}
