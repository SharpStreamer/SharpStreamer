using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.Kafka.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sharp_streamer");

            migrationBuilder.CreateTable(
                name: "published_events",
                schema: "sharp_streamer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "json", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3"),
                    EventKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_published_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "received_events",
                schema: "sharp_streamer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdateTimestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    Partition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Topic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "json", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3"),
                    EventKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_received_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_For_Publishing",
                schema: "sharp_streamer",
                table: "published_events",
                columns: new[] { "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "published_events",
                column: "Timestamp")
                .Annotation("Npgsql:IndexMethod", "BRIN");

            migrationBuilder.CreateIndex(
                name: "IX_EventKey_Status",
                schema: "sharp_streamer",
                table: "received_events",
                columns: new[] { "EventKey", "Status", "Timestamp" },
                filter: "\"Status\" = 0 or \"Status\" = 3 or \"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Events_For_Processing",
                schema: "sharp_streamer",
                table: "received_events",
                columns: new[] { "Status", "UpdateTimestamp", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedEvents_Timestamp",
                schema: "sharp_streamer",
                table: "received_events",
                column: "Timestamp")
                .Annotation("Npgsql:IndexMethod", "BRIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "published_events",
                schema: "sharp_streamer");

            migrationBuilder.DropTable(
                name: "received_events",
                schema: "sharp_streamer");
        }
    }
}
