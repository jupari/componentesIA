using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComponentesIA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleDocumentToTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SampleDocumentPath",
                table: "ExtractionTemplates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampleDocumentPath",
                table: "ExtractionTemplates");
        }
    }
}
