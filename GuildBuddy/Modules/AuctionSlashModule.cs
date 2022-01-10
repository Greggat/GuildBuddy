using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity;
using GuildBuddy.Models.AuctionModels;
using GuildBuddy.Models.Checks;
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
        private readonly GuildBuddyContext _db;
        private readonly AuctionService _auctionService;
        private readonly IBackgroundJobClient _backgroundJobs;

        public AuctionSlashModule(GuildBuddyContext db, AuctionService auctionService, IBackgroundJobClient backgroundJobs)
        {
            _db = db;
            _auctionService = auctionService;
            _backgroundJobs = backgroundJobs;
        }

        [SlashRequireAuctionChannelSet]
        [SlashRequireAuctionPermissions(AuctionPermissions.Create)]
        [SlashCommand("create", "Create a new auction listing.")]
        public async Task Create(InteractionContext ctx,
            [Option("name", "Name of item")] string itemName,
            [Option("expiration", "How many hours to list the auction for.")] long hours,
            [Option("minimum_inc", "The minimum bidding increments")] double minInc = 0,
            [Option("minimum_bid", "The minimum bid")] double minBid = 0,
            [Option("auto_extend", "Automatically extend when bidded on last 5 mins")] bool autoExtend = false
            )
        {
            var auction = _db.Auctions.Add(new Auction
            {
                GuildId = ctx.Guild.Id,
                Name = itemName,
                Expiration = DateTime.UtcNow.AddHours(hours),
                CurrentBid = minBid,
                MinIncrement = minInc,
                AutoExtend = autoExtend
            });
            try
            {
                await _db.SaveChangesAsync();
                var auctionId = (ulong)auction.Property("Id").CurrentValue;
                var jobId = _backgroundJobs.Schedule<AuctionService>(o => o.NotifyWinnerAsync(auctionId), TimeSpan.FromHours(hours));
                auction.Property("JobId").CurrentValue = jobId;
                await _db.SaveChangesAsync();
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Auction Created")
                .AddField("ID", auctionId.ToString(), true)
                .AddField("Name", itemName, true)
                .AddField("Expires", hours + "hr", true)
                .AddField("Minimum Increment", minInc.ToString(), true)
                .AddField("Auto Extend", autoExtend.ToString(), true)
                .AddField("Starting Bid", minBid.ToString())
                .WithFooter($"Use `/auction bid {auctionId} <bid amount>` to bid on this item.");

                var response = new DiscordInteractionResponseBuilder()
                    .AddEmbed(eb)
                    .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "notification_toggle", "Toggle Notifications"));

                await ctx.CreateResponseAsync(response);

                var notifyRole = await ctx.Guild.CreateRoleAsync($"AuctionNotify{auctionId}", mentionable: true);
            }
            catch (Exception ex)
            {
                await ctx.CreateResponseAsync("[Auction]: Failed to create auction.", true);
                Console.WriteLine(ex);
            }
        }

        [SlashRequireAuctionPermissions(AuctionPermissions.Bid)]
        [SlashCommand("bid", "description")]
        public async Task Bid(InteractionContext ctx,
            [Option("id", "Id of the item listing")] long auctionId,
            [Option("amount", "The amount you want to bid on the item")] double bidAmount)
        {
            var bidder = ctx.User;

            var auction = _db.Auctions.Where(auction =>
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

            if (bidAmount < 0)
            {
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Bid Failed")
                .WithDescription($"Bid amount must be greater than zero.");
                await ctx.CreateResponseAsync(eb, true);
                return;
            }

            if (bidAmount >= (auction.CurrentBid + auction.MinIncrement) || auction.BidderId == 0)
            {
                auction.BidderId = bidder.Id;
                auction.BidderName = bidder.Username;
                auction.CurrentBid = bidAmount;
                await _db.SaveChangesAsync();

                var notificationRole = _auctionService.GetNotificationRole(ctx.Guild, auction.Id);

                var response = new DiscordInteractionResponseBuilder();
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Auction Bid Received - ID: {auctionId}")
                .WithDescription($"{bidder.Username} bid {bidAmount} on {auction.Name}.")
                .WithFooter($"Use `/auction bid {auctionId} <bid amount>` to bid on this item.");

                response.AddEmbed(eb);
                if (notificationRole != null)
                {
                    response.WithContent(notificationRole.Mention);
                    response.AddMention(new RoleMention(notificationRole));
                }

                await ctx.CreateResponseAsync(response);

                if (notificationRole is not null)
                    await ctx.Member.GrantRoleAsync(notificationRole);

                if (auction.AutoExtend && DateTime.UtcNow.AddMinutes(5) > auction.Expiration)
                {
                    auction.Expiration = auction.Expiration.AddMinutes(5);
                    _backgroundJobs.Delete(auction.JobId);
                    var jobId = _backgroundJobs.Schedule<AuctionService>(o => o.NotifyWinnerAsync(auction.Id), auction.Expiration - DateTime.UtcNow);
                    auction.JobId = jobId;
                    await _db.SaveChangesAsync();

                    var extEb = new DiscordEmbedBuilder()
                    .WithTitle($"Auction Auto Extended")
                    .WithDescription($"{auction.Name} has been extended by 5 mins.")
                    .WithFooter($"Use `/auction bid {auction.Id} <bid amount>` to bid on this item.");

                    var noticeChannel = await _auctionService.GetNotificationChannelAsync(ctx.Guild.Id);
                    if (noticeChannel is not null)
                        await noticeChannel.SendMessageAsync(extEb);
                    else
                        await ctx.Channel.SendMessageAsync(extEb);                }
            }
            else
            {
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Bid Failed")
                .WithDescription($"You must increase the bid price by at least {auction.MinIncrement} for this auction!");
                await ctx.CreateResponseAsync(eb, true);
            }
        }

        [SlashRequireAuctionPermissions(AuctionPermissions.View)]
        [SlashCommand("display", "Display the active auctions.")]
        public async Task Display(InteractionContext ctx)
        {
            var auctions = _db.Auctions.Where(auction => auction.GuildId == ctx.Guild.Id && auction.Expiration > DateTime.UtcNow).ToList();

            if (auctions.Count > 0)
            {
                await ctx.Interaction.
                SendPaginatedResponseAsync(true, ctx.User, _auctionService.GenerateAuctionPages(auctions),
                behaviour: PaginationBehaviour.Ignore);
            }
            else
            {
                var eb = new DiscordEmbedBuilder()
                .WithTitle($"No Auctions Listed")
                .WithDescription($"There are currently no auctions listed on this server.");
                await ctx.CreateResponseAsync(eb, true);
            }
        }

        [SlashRequireAuctionPermissions(AuctionPermissions.Remove)]
        [SlashCommand("remove", "Remove an auction")]
        public async Task Remove(InteractionContext ctx, [Option("id", "Id of the auction to remove")] long id)
        {
            var auction = _db.Auctions.Where(auction => auction.Id == (ulong)id && auction.GuildId == ctx.Guild.Id).FirstOrDefault();
            if (auction is not null)
            {
                _db.Auctions.Remove(auction);
                await _db.SaveChangesAsync();
                _backgroundJobs.Delete(auction.JobId);

                var eb = new DiscordEmbedBuilder()
                .WithTitle($"Auction Deleted - ID: {id}")
                .WithDescription($"The auction for {auction.Name} was successfully removed.");
                await ctx.CreateResponseAsync(eb);

                var notifyRole = ctx.Guild.Roles.Values.Where(role => role.Name == $"AuctionNotify{auction.Id}").FirstOrDefault();
                if (notifyRole is not null)
                    await notifyRole.DeleteAsync();
            }
            else
                await ctx.CreateResponseAsync($"[Auction]: Could not find auction with Id: {id}.", true);
        }

        [SlashRequireUserPermissions(Permissions.Administrator)]
        [SlashCommand("setrolepermissions", "Configure the auction permissions for a role on the server.")]
        public async Task SetRolePermissions(InteractionContext ctx,
            [Option("role", "The role to add permissions to.")] DiscordRole role,
            [Option("permissions", "none|view|bid|create|remove|all|banned")] string permString)
        {
            var perms = permString.Split('|');
            AuctionPermissions permResult = 0;

            foreach (var perm in perms)
            {
                if (Enum.TryParse(typeof(AuctionPermissions), perm, true, out var parsedPerm))
                {
                    permResult |= (AuctionPermissions)parsedPerm;
                }
                else
                {
                    var eb = new DiscordEmbedBuilder()
                    .WithTitle($"Syntax Error")
                    .WithDescription($"{perm} is not a valid permissions.\nAvailable permissions are view|bid|create|remove|all|banned");
                    await ctx.CreateResponseAsync(eb);
                    return;
                }
            }

            var auctionRole = _db.AuctionRoles.Where(o => o.RoleId == role.Id && o.GuildId == ctx.Guild.Id).FirstOrDefault();

            if (auctionRole != null)
            {
                auctionRole.Permissions = permResult;
            }
            else
            {
                _db.AuctionRoles.Add(new AuctionRole
                {
                    GuildId = ctx.Guild.Id,
                    RoleId = role.Id,
                    Permissions = permResult
                });
            }

            await _db.SaveChangesAsync();

            var ebSuccess = new DiscordEmbedBuilder()
                    .WithTitle($"Auction Role Permissions Saved")
                    .WithDescription($"{role.Name} now contains the following permissions ({permResult}).");
            await ctx.CreateResponseAsync(ebSuccess);
        }

        [SlashRequireUserPermissions(Permissions.Administrator)]
        [SlashCommand("setchannel", "Sets the auction notifications channel.")]
        public async Task SetChannel(InteractionContext ctx)
        {
            var channel = _db.AuctionChannels.Where(o => o.GuildId == ctx.Guild.Id).FirstOrDefault();

            if (channel != null)
            {
                channel.ChannelId = ctx.Channel.Id;
            }
            else
            {
                _db.AuctionChannels.Add(new AuctionChannel { ChannelId = ctx.Channel.Id, GuildId = ctx.Guild.Id });
            }

            _auctionService.UpdateNotificationChannelCache(ctx.Guild.Id, ctx.Channel.Id);
            await _db.SaveChangesAsync();

            var eb = new DiscordEmbedBuilder()
                    .WithTitle($"Auction Notifications Channel Set")
                    .WithDescription($"Auction notifications will now be sent to {ctx.Channel.Name}.");
            await ctx.CreateResponseAsync(eb);
        }
    }
}
