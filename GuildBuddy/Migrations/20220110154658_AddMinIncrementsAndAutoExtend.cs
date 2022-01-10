using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildBuddy.Migrations
{
    public partial class AddMinIncrementsAndAutoExtend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoExtend",
                table: "Auctions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "MinIncrement",
                table: "Auctions",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoExtend",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "MinIncrement",
                table: "Auctions");
        }
    }
}
