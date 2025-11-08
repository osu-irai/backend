using Microsoft.EntityFrameworkCore.Migrations;
using osuRequestor.Models;

#nullable disable

namespace osuRequestor.Migrations
{
    /// <inheritdoc />
    public partial class AddSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_RequestedFromId",
                table: "Requests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:Source", "twitch,website");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedFromId",
                table: "Requests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<RequestSource>(
                name: "Source",
                table: "Requests",
                type: "\"Source\"",
                nullable: false,
                defaultValue: RequestSource.Website);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_RequestedFromId",
                table: "Requests",
                column: "RequestedFromId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_RequestedFromId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Requests");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:Source", "twitch,website");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedFromId",
                table: "Requests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_RequestedFromId",
                table: "Requests",
                column: "RequestedFromId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
