using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerceAPI.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmailUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rate_limit_policies",
                columns: new[] { "id", "enabled", "name", "permit_limit", "window_seconds" },
                values: new object[,]
                {
                    { 7, true, "OrderReadPolicy", 30, 60 },
                    { 8, true, "OrderWritePolicy", 5, 60 },
                    { 9, true, "OrderPatchPolicy", 10, 60 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DeleteData(
                table: "rate_limit_policies",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "rate_limit_policies",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "rate_limit_policies",
                keyColumn: "id",
                keyValue: 9);
        }
    }
}
