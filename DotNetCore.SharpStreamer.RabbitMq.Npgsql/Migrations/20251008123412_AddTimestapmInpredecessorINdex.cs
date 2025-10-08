using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddTimestapmInpredecessorINdex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.CreateIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events",
                columns: new[] { "EventKey", "Status", "Timestamp" },
                filter: "\"Status\" = 0 or \"Status\" = 3 or \"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.CreateIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events",
                columns: new[] { "EventKey", "Status" },
                filter: "\"Status\" = 0 or \"Status\" = 3 or \"Status\" = 1");
        }
    }
}
