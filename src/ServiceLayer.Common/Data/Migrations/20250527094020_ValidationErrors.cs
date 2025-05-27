using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLayer.Mesh.Migrations
{
    /// <inheritdoc />
    public partial class ValidationErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValidationErrors",
                table: "MeshFiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationErrors",
                table: "MeshFiles");
        }
    }
}
