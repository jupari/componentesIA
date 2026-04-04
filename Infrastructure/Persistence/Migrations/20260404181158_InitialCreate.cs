using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComponentesIA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExtractionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AiProvider = table.Column<string>(type: "text", nullable: true),
                    ModelName = table.Column<string>(type: "text", nullable: true),
                    PromptStrategy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalDocuments = table.Column<int>(type: "integer", nullable: false),
                    ProcessedDocuments = table.Column<int>(type: "integer", nullable: false),
                    FailedDocuments = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentBatches_ExtractionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExtractionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtractionFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    AliasesJson = table.Column<string>(type: "text", nullable: true),
                    ValidationRegex = table.Column<string>(type: "text", nullable: true),
                    ExampleValue = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    NormalizeRule = table.Column<string>(type: "text", nullable: true),
                    ConfidenceThreshold = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionFields_ExtractionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExtractionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentFiles_DocumentBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "DocumentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Provider = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    PromptVersion = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionJobs_DocumentFiles_DocumentFileId",
                        column: x => x.DocumentFileId,
                        principalTable: "DocumentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExtractionJobs_ExtractionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExtractionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtractionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawResponse = table.Column<string>(type: "text", nullable: true),
                    NormalizedJson = table.Column<string>(type: "text", nullable: true),
                    ValidationStatus = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionResults_ExtractionJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ExtractionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractionFieldResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawValue = table.Column<string>(type: "text", nullable: true),
                    NormalizedValue = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationMessage = table.Column<string>(type: "text", nullable: true),
                    PageNumber = table.Column<int>(type: "integer", nullable: true),
                    BoundingBoxJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionFieldResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionFieldResults_ExtractionResults_ResultId",
                        column: x => x.ResultId,
                        principalTable: "ExtractionResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBatches_TemplateId",
                table: "DocumentBatches",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFiles_BatchId",
                table: "DocumentFiles",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionFieldResults_ResultId",
                table: "ExtractionFieldResults",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionFields_TemplateId",
                table: "ExtractionFields",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_DocumentFileId",
                table: "ExtractionJobs",
                column: "DocumentFileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_TemplateId",
                table: "ExtractionJobs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionResults_JobId",
                table: "ExtractionResults",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionTemplates_Code",
                table: "ExtractionTemplates",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtractionFieldResults");

            migrationBuilder.DropTable(
                name: "ExtractionFields");

            migrationBuilder.DropTable(
                name: "ExtractionResults");

            migrationBuilder.DropTable(
                name: "ExtractionJobs");

            migrationBuilder.DropTable(
                name: "DocumentFiles");

            migrationBuilder.DropTable(
                name: "DocumentBatches");

            migrationBuilder.DropTable(
                name: "ExtractionTemplates");
        }
    }
}
