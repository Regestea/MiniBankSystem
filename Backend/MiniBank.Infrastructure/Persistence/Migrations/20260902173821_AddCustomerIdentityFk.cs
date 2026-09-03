using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdentityFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Same-Guid design integrity: a customer row must reference an existing IdentityUser.
            // DEFERRABLE INITIALLY DEFERRED lets EF insert both rows in a single SaveChanges
            // regardless of statement ordering within the transaction.
            migrationBuilder.Sql(
                """
                ALTER TABLE customers
                    ADD CONSTRAINT fk_customers_aspnet_user
                    FOREIGN KEY (customer_id)
                    REFERENCES "AspNetUsers" ("Id")
                    ON DELETE RESTRICT
                    DEFERRABLE INITIALLY DEFERRED;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE customers DROP CONSTRAINT IF EXISTS fk_customers_aspnet_user;");
        }
    }
}
