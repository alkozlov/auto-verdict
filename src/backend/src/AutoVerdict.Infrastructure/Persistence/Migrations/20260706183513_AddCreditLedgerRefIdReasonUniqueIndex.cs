using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVerdict.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditLedgerRefIdReasonUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_credit_ledger_entries_ReferenceId_Reason",
                table: "credit_ledger_entries",
                columns: new[] { "ReferenceId", "Reason" },
                unique: true,
                filter: "\"ReferenceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_credit_ledger_entries_ReferenceId_Reason",
                table: "credit_ledger_entries");
        }
    }
}
