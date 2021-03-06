using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using GuildBuddy.Models.AuctionModels;
using GuildBuddy.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Services
{
    public class AuctionService
    {
        private readonly DiscordClient _client;
        private ConcurrentDictionary<ulong, ulong> _auctionChannels;

        public AuctionService(DiscordClient client)
        {
            _client = client;

            using var db = new GuildBuddyContext();
            _auctionChannels = new ConcurrentDictionary<ulong, ulong>(db.AuctionChannels.ToDictionary(o => o.GuildId, o => o.ChannelId));

            _client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        public async Task<DiscordChannel> GetNotificationChannelAsync(ulong guildId)
        {
            var channelId = GetNotificationChannelIdFromCache(guildId);
            if (channelId != 0)
            {
                return await _client.GetChannelAsync(channelId);
            }
            return null;
        }

        public ulong GetNotificationChannelIdFromCache(ulong guildId)
        {
            if(_auctionChannels.TryGetValue(guildId, out ulong channelId))
                return channelId;
            return 0;
        }

        public void UpdateNotificationChannelCache(ulong guildId, ulong channelId)
        {
            if (_auctionChannels.ContainsKey(guildId))
            {
                _auctionChannels[guildId] = channelId;
            }
            else
            {
                _auctionChannels.TryAdd(guildId, channelId);
            }
        }

        public async Task NotifyWinnerAsync(ulong auctionId)
        {
            using var db = new GuildBuddyContext();
            var auction = db.Auctions.Where(o => o.Id == auctionId).FirstOrDefault();
            await db.DisposeAsync();

            try
            {
                var guild = await _client.GetGuildAsync(auction.GuildId);
                var notifyRole = guild.Roles.Values.Where(role => role.Name == $"AuctionNotify{auction.Id}").FirstOrDefault();

                var channel = await _client.GetChannelAsync(_auctionChannels[auction.GuildId]);
                if (auction is not null && channel is not null)
                {
                    if (auction.BidderId != 0)
                    {
                        var winner = await _client.GetUserAsync(auction.BidderId);
                        if (winner is not null)
                        {
                            var msg = new DiscordMessageBuilder();
                            var eb = new DiscordEmbedBuilder()
                            .WithTitle("Auction Won!")
                            .WithDescription($"{winner.Mention} has won {auction.Name} for {auction.CurrentBid}!")
                            .WithFooter($"ID: {auction.Id}");

                            msg.AddEmbed(eb);
                            if (notifyRole != null)
                            {
                                msg.WithContent(notifyRole.Mention);
                                msg.WithAllowedMention(new RoleMention(notifyRole));
                            }

                            await channel.SendMessageAsync(msg);
                        }
                    }
                    else
                    {
                        var eb = new DiscordEmbedBuilder()
                            .WithTitle("Auction Expired")
                            .WithDescription($"{auction.Name} expired with no bids!")
                            .WithFooter($"ID: {auction.Id}");
                        await channel.SendMessageAsync(eb);
                    }

                    if (notifyRole is not null)
                        await notifyRole.DeleteAsync();
                }
                else if (channel is null)
                    await SendChannelNotSetError(auction.GuildId);
            }
            catch (KeyNotFoundException e)
            {
                await SendChannelNotSetError(auction.GuildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task SendChannelNotSetError(ulong guildId)
        {
            var guild = await _client.GetGuildAsync(guildId);
            if (guild is not null)
            {
                await guild.Owner.SendMessageAsync("Attempted to send an auction notification to your discord server," +
                        " but your Auction Channel doesn't appear to be set. Please use /auction setchannel in the channel you wish to receieve notifications!");
            }
        }

        public IEnumerable<Page> GenerateAuctionPages(IEnumerable<Auction> auctions)
        {
            List<Page> result = new List<Page>();

            Queue<Auction> queue = new Queue<Auction>(auctions);
            while (queue.Count > 0)
            {
                Page page = new Page();
                StringBuilder contents = new StringBuilder();

                contents.Append(
                    "+-------+----------------------------+-----------------------------------+\n" +
                    "|                                Auctions                                |\n" +
                    "+-------+-----------------------------+-------------------------+--------+\n" +
                    "|  Id   |           Name              |            Bid          | Expire |\n" +
                    "+-------+-----------------------------+-------------------------+--------+\n"
                    );

                for (int i = 0; i < 5; i++)
                {
                    if (queue.Count > 0)
                    {
                        var auction = queue.Dequeue();
                        var biddingStr = (auction.CurrentBid + "(" + auction.BidderName + ")").PadBoth(26).Substring(0, 25);
                        var expire = Math.Round((auction.Expiration - DateTime.UtcNow).TotalHours);
                        var expireStr = (expire.ToString() + "hr").PadBoth(8);
                        contents.AppendLine($"|{auction.Id.ToString().PadBoth(7)}|{auction.Name.PadBoth(30).Substring(0, 29)}|{biddingStr}|{expireStr}|");
                        contents.AppendLine("+-------+-----------------------------+-------------------------+--------+");
                    }
                    else
                        break;
                }
                page.Content = Formatter.BlockCode(contents.ToString());
                result.Add(page);
            }

            return result;
            
        }

        public IEnumerable<Page> GenerateAuctionPagesV2(IEnumerable<Auction> auctions)
        {
            List<Page> result = new List<Page>();

            Queue<Auction> queue = new Queue<Auction>(auctions);
            while (queue.Count > 0)
            {
                Page page = new Page();
                StringBuilder ids = new StringBuilder();
                StringBuilder names = new StringBuilder();
                StringBuilder bidderNames = new StringBuilder();
                StringBuilder bids = new StringBuilder();
                for (int i = 0; i < 5; i++)
                {
                    if (queue.Count > 0)
                    {
                        var auction = queue.Dequeue();
                        ids.AppendLine(auction.Id.ToString());
                        names.AppendLine(auction.Name); 
                        bids.AppendLine(auction.CurrentBid.ToString() + $" - {auction.BidderName}");
                    }
                    else
                        break;
                }
                var eb = new DiscordEmbedBuilder()
                    .WithTitle("Auctions")
                    .AddField("ID", ids.ToString(), true)
                    .AddField("Name", names.ToString(), true)
                    .AddField("Bid", bids.ToString(), true);
                    //.AddField("test", "", true);
                page.Embed = eb.Build();
                result.Add(page);
            }

            return result;

        }

        public DiscordRole GetNotificationRole(DiscordGuild guild, ulong auctionId)
        {
            return guild.Roles.Values.Where(role => role.Name == $"AuctionNotify{auctionId}").FirstOrDefault();
        }

        private async Task OnComponentInteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            if(e.Id == "notification_toggle")
            {
                var fields = e.Message.Embeds.FirstOrDefault()?.Fields;
                if (fields == null)
                    return;

                var auctionIdField = fields.Where(field => field.Name == "ID").FirstOrDefault();
                if (auctionIdField == null)
                    return;

                if (!ulong.TryParse(auctionIdField.Value, out var auctionId))
                    return;

                var notifyRole = GetNotificationRole(e.Guild, auctionId);
                if (notifyRole == null)
                    return;

                var member = await e.Guild.GetMemberAsync(e.User.Id);

                DiscordEmbedBuilder eb;
                if(!member.Roles.Contains(notifyRole))
                {
                    await member.GrantRoleAsync(notifyRole);
                    eb = new DiscordEmbedBuilder()
                    .WithTitle("Auction Nofications")
                    .WithDescription("The notification role for this auction has been added to your profile.");
                }
                else
                {
                    await member.RevokeRoleAsync(notifyRole);
                    eb = new DiscordEmbedBuilder()
                    .WithTitle("Auction Nofications")
                    .WithDescription("The notification role for this auction has been removed from your profile.");
                }

                var response = new DiscordInteractionResponseBuilder().AddEmbed(eb).AsEphemeral(true);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,response);
            }
        }
    }
}
