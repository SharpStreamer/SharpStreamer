using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetCore.SharpStreamer.RabbitMq.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddNewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "sharp_streamer",
                table: "received_events",
                type: "integer",
                nullable: false,
                comment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "None = 0,Succeeded = 1,Failed = 2");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "sharp_streamer",
                table: "published_events",
                type: "integer",
                nullable: false,
                comment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "None = 0,Succeeded = 1,Failed = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "sharp_streamer",
                table: "received_events",
                type: "integer",
                nullable: false,
                comment: "None = 0,Succeeded = 1,Failed = 2",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "sharp_streamer",
                table: "published_events",
                type: "integer",
                nullable: false,
                comment: "None = 0,Succeeded = 1,Failed = 2",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "None = 0,InProgress = 1,Succeeded = 2,Failed = 3");
        }
    }
}
