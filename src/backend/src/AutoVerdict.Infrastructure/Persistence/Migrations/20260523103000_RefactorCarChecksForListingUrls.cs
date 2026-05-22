using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVerdict.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCarChecksForListingUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleIdentifier",
                table: "car_checks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentStorageKey",
                table: "car_checks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "car_checks",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListingUrl",
                table: "car_checks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE car_checks
                SET "ListingUrl" = "VehicleIdentifier"
                WHERE "ListingUrl" = ''
                """);

            migrationBuilder.AddColumn<string>(
                name: "Make",
                table: "car_checks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MileageKm",
                table: "car_checks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "car_checks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "car_checks",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotStorageKey",
                table: "car_checks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "car_checks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "car_checks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_car_checks_ListingUrl",
                table: "car_checks",
                column: "ListingUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_car_checks_ListingUrl",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "ListingUrl",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "Make",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "MileageKm",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "ScreenshotStorageKey",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "car_checks");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "car_checks");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleIdentifier",
                table: "car_checks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentStorageKey",
                table: "car_checks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
