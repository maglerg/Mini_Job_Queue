using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mini_Job_Queue.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLogAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LogAnalysisInputs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAnalysisInputs", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_LogAnalysisInputs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogAnalysisResults",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalLines = table.Column<int>(type: "integer", nullable: false),
                    ParsedLines = table.Column<int>(type: "integer", nullable: false),
                    InvalidLines = table.Column<int>(type: "integer", nullable: false),
                    InformationCount = table.Column<int>(type: "integer", nullable: false),
                    WarningCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    TopMessagesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAnalysisResults", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_LogAnalysisResults_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogAnalysisInputs");

            migrationBuilder.DropTable(
                name: "LogAnalysisResults");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Jobs");
        }
    }
}
