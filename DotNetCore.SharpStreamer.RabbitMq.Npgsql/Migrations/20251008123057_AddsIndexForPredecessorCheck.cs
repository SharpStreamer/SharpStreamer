using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddsIndexForPredecessorCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                schema: "sharp_streamer",
                table: "received_events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                schema: "sharp_streamer",
                table: "published_events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events",
                columns: new[] { "EventKey", "Status" },
                filter: "\"Status\" = 0 or \"Status\" = 3 or \"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropColumn(
                name: "EventKey",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropColumn(
                name: "EventKey",
                schema: "sharp_streamer",
                table: "published_events");
        }
    }
}
