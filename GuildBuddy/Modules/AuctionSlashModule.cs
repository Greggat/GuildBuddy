﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity;
using GuildBuddy.Data;
using DSharpPlus.Interactivity.Extensions;
using GuildBuddy.Services;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.Interactivity.Enums;
using Hangfire;

namespace GuildBuddy.Modules
{
    [SlashCommandGroup("auction", "An auction system for bidding on items.")]
    public class AuctionSlashModule : ApplicationCommandModule
    {
        private readonly AuctionService _auctionService;
        private readonly IBackgroundJobClient _backgroundJobs;

        public AuctionSlashModule(AuctionService auctionService, IBackgroundJobClient backgroundJobs)
        {
            _auctionService = auctionService;
            _backgroundJobs = backgroundJobs;
        }

        [SlashRequirePermissions(Permissions.Administrator)]
        [SlashCommand("create", "Create a new auction listing.")]
        public async Task Create(InteractionContext ctx, 
            [Option("name", "Name of item")] string itemName, 
            [Option("expiration", "How many hours to list the auction for.")] long hours,
            [Option("minimum_bid", "The minimum bid")] double minBid = 0)
        {
            using var db = new GuildBuddyContext();
            var auction = db.Auctions.Add(new Auction
            {
                GuildId = ctx.Guild.Id,
                Name = itemName,
                Expiration = DateTime.UtcNow.AddHours(hours),
                CurrentBid = minBid
            });
            try
            {
                await db.SaveChangesAsync();
                var auctionId = (ulong)auction.Property("Id").CurrentValue;
                var jobId = _backgroundJobs.Schedule<AuctionService>(o => o.NotifyWinnerAsync(auctionId), TimeSpan.FromHours(hours));
                auction.Property("JobId").CurrentValue = jobId;
                await db.SaveChangesAsync();
                await db.DisposeAsync();
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Auction Created - ID: {auctionId}")
                .WithDescription($"Name: {itemName}\nExpires in {hours} hours.")
                .WithFooter($"Use `/auction bid {auctionId} <bid amount>` to bid on this item.");
                await ctx.CreateResponseAsync(eb);
                
            }
            catch (Exception ex)
            {
                await ctx.CreateResponseAsync("[Auction]: Failed to create auction.", true);
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

            var auction = db.Auctions.Where(auction => 
                                        auction.GuildId == ctx.Guild.Id && 
                                        auction.Id == (ulong)auctionId &&
                                        auction.Expiration > DateTime.UtcNow).FirstOrDefault();

            if (auction == null)
            {
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Bid Failed")
                .WithDescription($"The auction you are trying to bid on does not exist.");
                await ctx.CreateResponseAsync(eb, true);
                return;
            }

            if(bidAmount < 0)
            {
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Bid Failed")
                .WithDescription($"Bid amount must be greater than zero.");
                await ctx.CreateResponseAsync(eb, true);
                return;
            }

            if (bidAmount > (auction.CurrentBid * 1.10) || auction.BidderId == 0)
            {
                auction.BidderId = bidder.Id;
                auction.BidderName = bidder.Username;
                auction.CurrentBid = bidAmount;
                await db.SaveChangesAsync();
                await db.DisposeAsync();
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Auction Bid Received - ID: {auctionId}")
                .WithDescription($"{bidder.Username} bid {bidAmount} on {auction.Name}.")
                .WithFooter($"Use `/auction bid {auctionId} <bid amount>` to bid on this item.");
                await ctx.CreateResponseAsync(eb);
            }
            else
            {
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Bid Failed")
                .WithDescription($"Bid must be at least 10% higher than current bid!");
                await ctx.CreateResponseAsync(eb, true);
            }
        }

        [SlashCommand("display", "Display the active auctions.")]
        public async Task Display(InteractionContext ctx)
        {
            using var db = new GuildBuddyContext();

            var auctions = db.Auctions.Where(auction => auction.GuildId == ctx.Guild.Id && auction.Expiration > DateTime.UtcNow).ToList();
            await db.DisposeAsync();

            await ctx.Interaction.
                SendPaginatedResponseAsync(true, ctx.User, _auctionService.GenerateAuctionPages(auctions), 
                behaviour: PaginationBehaviour.Ignore);
        }

        [SlashRequirePermissions(Permissions.Administrator)]
        [SlashCommand("remove", "Remove an auction")]
        public async Task Remove(InteractionContext ctx,[Option("id", "Id of the auction to remove")] long id)
        {
            using var db = new GuildBuddyContext();

            var auction = db.Auctions.Where(auction => auction.Id == (ulong)id && auction.GuildId == ctx.Guild.Id).FirstOrDefault();
            if (auction is not null)
            {
                db.Auctions.Remove(auction);
                await db.SaveChangesAsync();
                await db.DisposeAsync();
                _backgroundJobs.Delete(auction.JobId);

                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Auction Deleted - ID: {id}")
                .WithDescription($"The auction for {auction.Name} was successfully removed.");
                await ctx.CreateResponseAsync(eb);
            }
            else
                await ctx.CreateResponseAsync($"[Auction]: Could not find auction with Id: {id}.", true);
        }
    }
}
