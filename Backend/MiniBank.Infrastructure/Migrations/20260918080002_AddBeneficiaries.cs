using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beneficiaries",
                columns: table => new
                {
                    beneficiary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    holder_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beneficiaries", x => x.beneficiary_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_beneficiaries_owner",
                table: "beneficiaries",
                column: "owner_customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_beneficiaries_owner_number",
                table: "beneficiaries",
                columns: new[] { "owner_customer_id", "account_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beneficiaries");
        }
    }
}
