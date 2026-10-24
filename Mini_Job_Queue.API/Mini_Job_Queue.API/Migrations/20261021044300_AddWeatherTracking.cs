using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mini_Job_Queue.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherTrackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationName = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherTrackings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeatherTrackingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemperatureCelsius = table.Column<double>(type: "double precision", nullable: false),
                    RelativeHumidityPercent = table.Column<int>(type: "integer", nullable: true),
                    PrecipitationMm = table.Column<double>(type: "double precision", nullable: true),
                    WindSpeedKmh = table.Column<double>(type: "double precision", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherRecords_WeatherTrackings_WeatherTrackingId",
                        column: x => x.WeatherTrackingId,
                        principalTable: "WeatherTrackings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRecords_WeatherTrackingId",
                table: "WeatherRecords",
                column: "WeatherTrackingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherRecords");

            migrationBuilder.DropTable(
                name: "WeatherTrackings");
        }
    }
}
