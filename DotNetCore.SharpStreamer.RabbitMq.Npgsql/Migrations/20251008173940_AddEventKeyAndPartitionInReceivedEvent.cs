using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddEventKeyAndPartitionInReceivedEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Partition",
                schema: "sharp_streamer",
                table: "received_events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Partition",
                schema: "sharp_streamer",
                table: "received_events");
        }
    }
}
