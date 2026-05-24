using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVerdict.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrawlerJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crawler_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    RawData = table.Column<string>(type: "jsonb", nullable: true),
                    NormalizedData = table.Column<string>(type: "jsonb", nullable: true),
                    ScreenshotBucket = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ScreenshotObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ScreenshotContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScreenshotSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsRetryable = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crawler_jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crawler_jobs_CreatedAt",
                table: "crawler_jobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crawler_jobs_ListingUrl",
                table: "crawler_jobs",
                column: "ListingUrl");

            migrationBuilder.CreateIndex(
                name: "IX_crawler_jobs_Status",
                table: "crawler_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_crawler_jobs_UserId",
                table: "crawler_jobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crawler_jobs");
        }
    }
}
