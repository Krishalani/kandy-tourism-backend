using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KandyTourism.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripLifecycleAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "Itineraries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                table: "Itineraries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Itineraries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Planning");

            migrationBuilder.CreateTable(
                name: "TripFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItineraryId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripFeedback", x => x.Id);
                    table.CheckConstraint("CK_TripFeedback_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_TripFeedback_Itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "Itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripStopRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripFeedbackId = table.Column<int>(type: "int", nullable: false),
                    AttractionId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopRatings", x => x.Id);
                    table.CheckConstraint("CK_TripStopRatings_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_TripStopRatings_Attractions_AttractionId",
                        column: x => x.AttractionId,
                        principalTable: "Attractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripStopRatings_TripFeedback_TripFeedbackId",
                        column: x => x.TripFeedbackId,
                        principalTable: "TripFeedback",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Itineraries_Status",
                table: "Itineraries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TripFeedback_ItineraryId",
                table: "TripFeedback",
                column: "ItineraryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripFeedback_SubmittedAtUtc",
                table: "TripFeedback",
                column: "SubmittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopRatings_AttractionId",
                table: "TripStopRatings",
                column: "AttractionId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopRatings_TripFeedbackId_AttractionId",
                table: "TripStopRatings",
                columns: new[] { "TripFeedbackId", "AttractionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripStopRatings");

            migrationBuilder.DropTable(
                name: "TripFeedback");

            migrationBuilder.DropIndex(
                name: "IX_Itineraries_Status",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Itineraries");
        }
    }
}
