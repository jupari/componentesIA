using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComponentesIA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Permissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Permissions");
        }
    }
}
