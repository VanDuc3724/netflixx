using Microsoft.EntityFrameworkCore.Migrations;

namespace Netflixx.Migrations
{
    public partial class history_details : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "ProductionManagerHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details",
                table: "ProductionManagerHistories");
        }
    }
}
