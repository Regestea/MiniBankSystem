using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Link_User_To_Customer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "AspNetUsers",
                newName: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_customer",
                table: "AspNetUsers",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_customer",
                table: "AspNetUsers",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_customer",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "ix_users_customer",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "AspNetUsers",
                newName: "CustomerId");
        }
    }
}
