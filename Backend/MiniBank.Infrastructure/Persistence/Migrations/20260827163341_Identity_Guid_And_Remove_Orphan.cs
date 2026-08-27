using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_Guid_And_Remove_Orphan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_customer",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "ix_users_customer",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "AspNetUsers");

            // Drop FKs that reference Identity PKs before altering column types (Postgres requires it)
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserClaims_AspNetUsers_UserId", table: "AspNetUserClaims");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserLogins_AspNetUsers_UserId", table: "AspNetUserLogins");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserRoles_AspNetUsers_UserId", table: "AspNetUserRoles");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserRoles_AspNetRoles_RoleId", table: "AspNetUserRoles");
            migrationBuilder.DropForeignKey(name: "FK_AspNetRoleClaims_AspNetRoles_RoleId", table: "AspNetRoleClaims");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserTokens_AspNetUsers_UserId", table: "AspNetUserTokens");

            // Use USING clause for text -> uuid cast. Tables are empty in fresh DB and existing string GUIDs are UUID-compatible.
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserTokens"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserRoles"" ALTER COLUMN ""RoleId"" TYPE uuid USING ""RoleId""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserRoles"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserLogins"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserClaims"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetRoles"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetRoleClaims"" ALTER COLUMN ""RoleId"" TYPE uuid USING ""RoleId""::uuid;");

            // Re-add FKs with uuid types
            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserClaims_AspNetUsers_UserId", table: "AspNetUserClaims");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserLogins_AspNetUsers_UserId", table: "AspNetUserLogins");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserRoles_AspNetUsers_UserId", table: "AspNetUserRoles");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserRoles_AspNetRoles_RoleId", table: "AspNetUserRoles");
            migrationBuilder.DropForeignKey(name: "FK_AspNetRoleClaims_AspNetRoles_RoleId", table: "AspNetRoleClaims");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUserTokens_AspNetUsers_UserId", table: "AspNetUserTokens");

            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserTokens"" ALTER COLUMN ""UserId"" TYPE text USING ""UserId""::text;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""Id"" TYPE text USING ""Id""::text;");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserRoles"" ALTER COLUMN ""RoleId"" TYPE text USING ""RoleId""::text;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserRoles"" ALTER COLUMN ""UserId"" TYPE text USING ""UserId""::text;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserLogins"" ALTER COLUMN ""UserId"" TYPE text USING ""UserId""::text;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUserClaims"" ALTER COLUMN ""UserId"" TYPE text USING ""UserId""::text;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetRoles"" ALTER COLUMN ""Id"" TYPE text USING ""Id""::text;");
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetRoleClaims"" ALTER COLUMN ""RoleId"" TYPE text USING ""RoleId""::text;");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
    }
}
