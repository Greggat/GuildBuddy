using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildBuddy.Migrations
{
    public partial class JobId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "Auctions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Auctions");
        }
    }
}
