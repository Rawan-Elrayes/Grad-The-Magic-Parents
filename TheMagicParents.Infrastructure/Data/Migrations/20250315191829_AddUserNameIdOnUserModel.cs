using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMagicParents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNameIdOnUserModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserNameId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserNameId",
                table: "AspNetUsers");
        }
    }
}
