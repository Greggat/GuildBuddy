using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildBuddy.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionChannels",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionChannels", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "AuctionRoles",
                columns: table => new
                {
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<short>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRoles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentBid = table.Column<double>(type: "REAL", nullable: false),
                    BidderId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BidderName = table.Column<string>(type: "TEXT", nullable: false),
                    Expiration = table.Column<DateTime>(type: "TEXT", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auctions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRoles_GuildId",
                table: "AuctionRoles",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_GuildId",
                table: "Auctions",
                column: "GuildId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionChannels");

            migrationBuilder.DropTable(
                name: "AuctionRoles");

            migrationBuilder.DropTable(
                name: "Auctions");
        }
    }
}
