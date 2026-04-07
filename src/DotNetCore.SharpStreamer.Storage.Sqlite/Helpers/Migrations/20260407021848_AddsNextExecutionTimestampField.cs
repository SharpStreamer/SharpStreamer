using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.Storage.Sqlite.Helpers.Migrations
{
    /// <inheritdoc />
    public partial class AddsNextExecutionTimestampField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextExecutionTimestamp",
                table: "received_events",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Events_For_Processing_New",
                table: "received_events",
                columns: new[] { "Status", "NextExecutionTimestamp", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_For_Processing_New",
                table: "received_events");

            migrationBuilder.DropColumn(
                name: "NextExecutionTimestamp",
                table: "received_events");
        }
    }
}
