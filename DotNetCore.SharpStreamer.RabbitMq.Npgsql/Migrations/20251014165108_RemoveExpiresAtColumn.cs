using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExpiresAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "sharp_streamer",
                table: "received_events");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "sharp_streamer",
                table: "published_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                schema: "sharp_streamer",
                table: "received_events",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                schema: "sharp_streamer",
                table: "published_events",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
