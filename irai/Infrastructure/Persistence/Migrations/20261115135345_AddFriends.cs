using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace irai.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261115135345_AddFriends : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "FriendsOnly",
            table: "Settings",
            type: "boolean",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Friends",
            columns: table => new
            {
                UserId = table.Column<int>(type: "integer", nullable: false),
                FriendId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Friends", x => new { x.UserId, x.FriendId });
                table.ForeignKey(
                    name: "FK_Friends_Users_UserId",
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
            name: "Friends");

        migrationBuilder.DropColumn(
            name: "FriendsOnly",
            table: "Settings");
    }
}
