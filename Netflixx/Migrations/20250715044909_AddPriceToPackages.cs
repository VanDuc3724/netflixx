using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Netflixx.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceToPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Packages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Packages");
        }
    }
}
