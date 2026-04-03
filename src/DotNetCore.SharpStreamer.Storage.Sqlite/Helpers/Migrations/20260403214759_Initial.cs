using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.Storage.Sqlite.Helpers.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "published_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, comment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3"),
                    EventKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_published_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "received_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Group = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdateTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Partition = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, comment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3"),
                    EventKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_received_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_For_Publishing",
                table: "published_events",
                columns: new[] { "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublishedEvents_Timestamp",
                table: "published_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EventKey_Status",
                table: "received_events",
                columns: new[] { "EventKey", "Status", "Timestamp" },
                filter: "\"Status\" = 0 OR \"Status\" = 3 OR \"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Events_For_Processing",
                table: "received_events",
                columns: new[] { "Status", "UpdateTimestamp", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedEvents_Timestamp",
                table: "received_events",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "published_events");

            migrationBuilder.DropTable(
                name: "received_events");
        }
    }
}
