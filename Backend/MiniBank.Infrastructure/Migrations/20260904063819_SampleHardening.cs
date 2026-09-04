using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SampleHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_transactions_occurred_on",
                table: "transactions",
                column: "occurred_on");

            migrationBuilder.CreateIndex(
                name: "ix_kyc_primary_document",
                table: "kyc_verifications",
                column: "primary_document_id");

            migrationBuilder.AddForeignKey(
                name: "fk_accounts_customer",
                table: "accounts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_accounts_customer",
                table: "accounts");

            migrationBuilder.DropIndex(
                name: "ix_transactions_occurred_on",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_kyc_primary_document",
                table: "kyc_verifications");
        }
    }
}
