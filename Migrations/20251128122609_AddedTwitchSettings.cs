using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace osuRequestor.Migrations
{
    /// <inheritdoc />
    public partial class AddedTwitchSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Twitch",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TwitchId = table.Column<int>(type: "integer", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Twitch", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Twitch_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Twitch");
        }
    }
}
