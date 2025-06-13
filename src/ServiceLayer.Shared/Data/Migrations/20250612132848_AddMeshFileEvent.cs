using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLayer.Mesh.Migrations
{
    /// <inheritdoc />
    public partial class AddMeshFileEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeshFileEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeshFileEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_MeshFileEvents_MeshFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "MeshFiles",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeshFileEvents_FileId",
                table: "MeshFileEvents",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeshFileEvents");
        }
    }
}
