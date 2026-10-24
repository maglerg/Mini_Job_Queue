using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mini_Job_Queue.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherTracking2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WeatherTrackingId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_WeatherTrackingId",
                table: "Jobs",
                column: "WeatherTrackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_WeatherTrackings_WeatherTrackingId",
                table: "Jobs",
                column: "WeatherTrackingId",
                principalTable: "WeatherTrackings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_WeatherTrackings_WeatherTrackingId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_WeatherTrackingId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WeatherTrackingId",
                table: "Jobs");
        }
    }
}
