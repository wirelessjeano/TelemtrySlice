using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelemetrySlice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_CustomerId_DeviceId_RecordedAt",
                table: "TelemetryEvents",
                columns: new[] { "CustomerId", "DeviceId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelemetryEvents_CustomerId_DeviceId_RecordedAt",
                table: "TelemetryEvents");
        }
    }
}
