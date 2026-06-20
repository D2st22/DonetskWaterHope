using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectsDonetskWaterHope.Migrations
{
    /// <inheritdoc />
    public partial class AddIotDeviceStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IotDeviceStatuses",
                columns: table => new
                {
                    IotDeviceStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    RawSensorValue = table.Column<int>(type: "integer", nullable: false),
                    FlowRate = table.Column<int>(type: "integer", nullable: false),
                    TotalCounter = table.Column<int>(type: "integer", nullable: false),
                    LeakageDetected = table.Column<bool>(type: "boolean", nullable: false),
                    WifiRssi = table.Column<int>(type: "integer", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "text", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IotDeviceStatuses", x => x.IotDeviceStatusId);
                    table.ForeignKey(
                        name: "FK_IotDeviceStatuses_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IotDeviceStatuses_DeviceId",
                table: "IotDeviceStatuses",
                column: "DeviceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IotDeviceStatuses");

        }
    }
}
