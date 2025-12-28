using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsDonetskWaterHope.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationByUserToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegisteredByUserId",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_RegisteredByUserId",
                table: "Devices",
                column: "RegisteredByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Users_RegisteredByUserId",
                table: "Devices",
                column: "RegisteredByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Users_RegisteredByUserId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_RegisteredByUserId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RegisteredByUserId",
                table: "Devices");
        }
    }
}
