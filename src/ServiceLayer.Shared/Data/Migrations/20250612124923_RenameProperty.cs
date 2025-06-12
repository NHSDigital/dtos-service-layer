using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLayer.Mesh.Migrations
{
    /// <inheritdoc />
    public partial class RenameProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AppointmenId",
                table: "NbssAppointmentEvents",
                newName: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AppointmentId",
                table: "NbssAppointmentEvents",
                newName: "AppointmenId");
        }
    }
}
