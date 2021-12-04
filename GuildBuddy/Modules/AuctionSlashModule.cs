using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GuildBuddy.Data;
using GuildBuddy.Util;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Modules
{
    [SlashCommandGroup("auction", "An auction system for bidding on items.")]
    public class AuctionSlashModule : ApplicationCommandModule
    {
        [SlashCommand("create", "Create a new auction listing.")]
        public async Task Create(InteractionContext ctx, 
            [Option("name", "Name of item")] string itemName, 
            [Option("expiration", "How many hours to list the auction for.")] long hours)
        {
            using var db = new GuildBuddyContext();
            var auction = db.Auctions.Add(new Auction
            {
                GuildId = ctx.Guild.Id,
                Name = itemName,
                Expiration = DateTime.UtcNow.AddHours(hours)
            });
            try
            {
                await db.SaveChangesAsync();
                var auctionId = auction.Member("Id").CurrentValue;
                var eb = new DiscordEmbedBuilder()
                .WithTitle("Auction Created")
                .WithDescription($"Id: {auctionId}\nName: {itemName}\nExpires in {hours} hours.")
                .WithFooter($"Use `/auction bid {auctionId} <bid amount>` to bid on this item.");
                await ctx.CreateResponseAsync(eb);
                //do hangfire for expiration
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [SlashCommand("bid", "description")]
        public async Task Bid(InteractionContext ctx, 
            [Option("id", "Id of the item listing")] long auctionId, 
            [Option("amount", "The amount you want to bid on the item")] double bidAmount)
        {
            using var db = new GuildBuddyContext();
            var bidder = ctx.User;

            var item = db.Auctions.Where(auction => 
                                        auction.GuildId == ctx.Guild.Id && 
                                        auction.Id == (ulong)auctionId &&
                                        auction.Expiration > DateTime.UtcNow).FirstOrDefault();

            if (item == null)
            {
                await ctx.CreateResponseAsync("The auction you are trying to bid on does not exist.");
                return;
            }

            if (bidAmount > (item.CurrentBid * 1.10))
            {
                item.BidderId = bidder.Id;
                item.BidderName = bidder.Username;
                item.CurrentBid = bidAmount;
                await db.SaveChangesAsync();
                await ctx.CreateResponseAsync($"[Auction]{bidder.Mention} bid {bidAmount} on {item.Name}.");
            }
            else
            {
                await ctx.CreateResponseAsync("[Auction] Error: Bid must be at least 10% higher than current bid!");
            }
        }

        [SlashCommand("display", "Display the active auctions.")]
        public async Task Display(InteractionContext ctx)
        {
            //TODO: Add pagnation for large auctions
            using var db = new GuildBuddyContext();

            var auctions = db.Auctions.Where(auction => auction.GuildId == ctx.Guild.Id && auction.Expiration > DateTime.UtcNow).ToList();

            StringBuilder response = new StringBuilder();
            response.Append(
                "+-------+----------------------------+-----------------------------------+\n" +
                "|                                 Auctions                               |\n" +
                "+-------+-----------------------------+----------------------------------+\n" +
                "|  Id   |           Name              |            Bid          | Expire |\n" +
                "+-------+-----------------------------+----------------------------------+\n"
                );
            foreach (var auction in auctions)
            {
                var biddingStr = (auction.CurrentBid + "(" + auction.BidderName + ")").PadRight(26).Substring(0,25);
                var expire = Math.Round((auction.Expiration - DateTime.UtcNow).TotalHours);
                var expireStr = (expire.ToString() + "hr").PadBoth(8);
                response.AppendLine($"|{auction.Id.ToString().PadBoth(7)}|{auction.Name.PadRight(30).Substring(0,29)}|{biddingStr}|{expireStr}|");
                response.AppendLine("+-------+-----------------------------+----------------------------------+");
            }
            
            await ctx.CreateResponseAsync(DSharpPlus.Formatter.BlockCode(response.ToString()));
        }
    }
}
