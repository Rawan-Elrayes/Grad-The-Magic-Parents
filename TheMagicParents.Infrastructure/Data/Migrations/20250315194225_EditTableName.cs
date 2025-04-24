using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMagicParents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Goverments_GovermentId",
                table: "Cities");

            migrationBuilder.DropTable(
                name: "Goverments");

            migrationBuilder.RenameColumn(
                name: "GovermentId",
                table: "Cities",
                newName: "GovernorateId");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_GovermentId",
                table: "Cities",
                newName: "IX_Cities_GovernorateId");

            migrationBuilder.CreateTable(
                name: "Governorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governorates", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Governorates_GovernorateId",
                table: "Cities",
                column: "GovernorateId",
                principalTable: "Governorates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Governorates_GovernorateId",
                table: "Cities");

            migrationBuilder.DropTable(
                name: "Governorates");

            migrationBuilder.RenameColumn(
                name: "GovernorateId",
                table: "Cities",
                newName: "GovermentId");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_GovernorateId",
                table: "Cities",
                newName: "IX_Cities_GovermentId");

            migrationBuilder.CreateTable(
                name: "Goverments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goverments", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Goverments_GovermentId",
                table: "Cities",
                column: "GovermentId",
                principalTable: "Goverments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
