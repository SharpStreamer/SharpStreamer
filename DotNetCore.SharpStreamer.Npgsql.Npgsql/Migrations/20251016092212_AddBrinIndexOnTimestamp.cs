using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.Npgsql.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddBrinIndexOnTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ReceivedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "received_events",
                column: "Timestamp")
                .Annotation("Npgsql:IndexMethod", "BRIN");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "published_events",
                column: "Timestamp")
                .Annotation("Npgsql:IndexMethod", "BRIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReceivedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropIndex(
                name: "IX_PublishedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "published_events");
        }
    }
}
