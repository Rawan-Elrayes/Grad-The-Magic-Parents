using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMagicParents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelledBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancelledBy",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancelledBy",
                table: "Bookings");
        }
    }
}
