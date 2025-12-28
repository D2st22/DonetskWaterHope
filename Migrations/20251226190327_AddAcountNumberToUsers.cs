using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsDonetskWaterHope.Migrations
{
    /// <inheritdoc />
    public partial class AddAcountNumberToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Users");
        }
    }
}
