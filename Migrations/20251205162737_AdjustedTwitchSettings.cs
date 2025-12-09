using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace osuRequestor.Migrations
{
    /// <inheritdoc />
    public partial class AdjustedTwitchSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Twitch");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Twitch",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableTwitch",
                table: "Settings",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Twitch");

            migrationBuilder.DropColumn(
                name: "EnableTwitch",
                table: "Settings");

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Twitch",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
