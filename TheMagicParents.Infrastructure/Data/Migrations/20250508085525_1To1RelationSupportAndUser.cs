using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMagicParents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class _1To1RelationSupportAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Supports_Client_SupportID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Supports_SupportID",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Client_SupportID",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SupportID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Client_SupportID",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "SupportID",
                table: "AspNetUsers",
                newName: "SupportId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SupportId",
                table: "AspNetUsers",
                column: "SupportId",
                unique: true,
                filter: "[SupportId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Supports_SupportId",
                table: "AspNetUsers",
                column: "SupportId",
                principalTable: "Supports",
                principalColumn: "SupportID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Supports_SupportId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SupportId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "SupportId",
                table: "AspNetUsers",
                newName: "SupportID");

            migrationBuilder.AddColumn<int>(
                name: "Client_SupportID",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Client_SupportID",
                table: "AspNetUsers",
                column: "Client_SupportID");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SupportID",
                table: "AspNetUsers",
                column: "SupportID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Supports_Client_SupportID",
                table: "AspNetUsers",
                column: "Client_SupportID",
                principalTable: "Supports",
                principalColumn: "SupportID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Supports_SupportID",
                table: "AspNetUsers",
                column: "SupportID",
                principalTable: "Supports",
                principalColumn: "SupportID");
        }
    }
}
