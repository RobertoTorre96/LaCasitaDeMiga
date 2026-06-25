using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaCasitaDeMiga.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityIsFeaturedAndVersionToVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "product_variants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "product_variants",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_featured",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "version",
                table: "product_variants");
        }
    }
}
