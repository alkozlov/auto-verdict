using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVerdict.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreditsGranted = table.Column<int>(type: "integer", nullable: false),
                    ExternalOrderId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_orders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_orders_ExternalOrderId",
                table: "payment_orders",
                column: "ExternalOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_orders_UserId",
                table: "payment_orders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "payment_orders");
        }
    }
}
