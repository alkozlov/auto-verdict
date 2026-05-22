using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVerdict.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarReportsAndAiRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_requests_car_checks_CarCheckId",
                        column: x => x.CarCheckId,
                        principalTable: "car_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "car_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportData = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_car_reports_car_checks_CarCheckId",
                        column: x => x.CarCheckId,
                        principalTable: "car_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_requests_CarCheckId",
                table: "ai_requests",
                column: "CarCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_car_reports_CarCheckId",
                table: "car_reports",
                column: "CarCheckId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_requests");

            migrationBuilder.DropTable(
                name: "car_reports");
        }
    }
}
