using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KandyTourism.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripDateAndShareLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShareId",
                table: "Itineraries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TripDate",
                table: "Itineraries",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Itineraries_ShareId",
                table: "Itineraries",
                column: "ShareId",
                unique: true,
                filter: "[ShareId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Itineraries_ShareId",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "ShareId",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "TripDate",
                table: "Itineraries");
        }
    }
}
