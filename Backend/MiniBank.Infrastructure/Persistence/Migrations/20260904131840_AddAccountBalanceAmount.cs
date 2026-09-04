using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountBalanceAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "balance_amount",
                table: "accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "balance_amount",
                table: "accounts");
        }
    }
}
