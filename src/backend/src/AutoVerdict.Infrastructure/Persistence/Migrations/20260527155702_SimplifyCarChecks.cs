using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVerdict.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyCarChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop dependent tables before dropping car_checks
            migrationBuilder.Sql("DROP TABLE IF EXISTS car_reports CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ai_requests CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS car_checks CASCADE");

            migrationBuilder.CreateTable(
                name: "car_checks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ListingUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserImageKeysJson = table.Column<string>(type: "text", nullable: true),
                    AnalysisStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_car_checks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_car_checks_CheckId",
                table: "car_checks",
                column: "CheckId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_car_checks_UserId",
                table: "car_checks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_car_checks_Status",
                table: "car_checks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "car_checks");
        }
    }
}
