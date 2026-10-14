using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mini_Job_Queue.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLogCategories4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorCount",
                table: "LogAnalysisResults");

            migrationBuilder.DropColumn(
                name: "InformationCount",
                table: "LogAnalysisResults");

            migrationBuilder.DropColumn(
                name: "WarningCount",
                table: "LogAnalysisResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErrorCount",
                table: "LogAnalysisResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InformationCount",
                table: "LogAnalysisResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarningCount",
                table: "LogAnalysisResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
