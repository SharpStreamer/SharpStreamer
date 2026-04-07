using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Helpers.Migrations
{
    /// <inheritdoc />
    public partial class AddsNextExecutionTimestampField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextExecutionTimestamp",
                schema: "sharp_streamer",
                table: "received_events",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Events_For_Processing_New",
                schema: "sharp_streamer",
                table: "received_events",
                columns: new[] { "Status", "NextExecutionTimestamp", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_For_Processing_New",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropColumn(
                name: "NextExecutionTimestamp",
                schema: "sharp_streamer",
                table: "received_events");
        }
    }
}
