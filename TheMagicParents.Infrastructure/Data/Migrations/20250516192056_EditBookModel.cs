using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheMagicParents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditBookModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Cities_CityID",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CityID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CityID",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "BookingTime",
                table: "Bookings",
                newName: "Day");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Houre",
                table: "Bookings",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Houre",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "Day",
                table: "Bookings",
                newName: "BookingTime");

            migrationBuilder.AddColumn<int>(
                name: "CityID",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CityID",
                table: "Bookings",
                column: "CityID");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Cities_CityID",
                table: "Bookings",
                column: "CityID",
                principalTable: "Cities",
                principalColumn: "Id");
        }
    }
}
