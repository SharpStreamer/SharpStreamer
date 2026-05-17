using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Helpers.Migrations
{
    /// <inheritdoc />
    public partial class DistributedOperatoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReceivedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropIndex(
                name: "IX_PublishedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "published_events");

            migrationBuilder.CreateTable(
                name: "distributed_operations",
                schema: "sharp_streamer",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distributed_operations", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "received_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "published_events",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "distributed_operations",
                schema: "sharp_streamer");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropIndex(
                name: "IX_PublishedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "published_events");

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
    }
}
