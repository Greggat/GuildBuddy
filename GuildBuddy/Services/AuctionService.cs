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
        public AuctionService(DiscordClient client)
        {
            _client = client;
        }

        public void NotifyWinner(ulong auctionId) => NotifyWinnerAsync(auctionId).Wait();
        public async Task NotifyWinnerAsync(ulong auctionId)
        {
            try
            {
                var channel = await _client.GetChannelAsync(688586052000022539);
                using var db = new GuildBuddyContext();
                var auction = db.Auctions.Where(o => o.Id == auctionId).FirstOrDefault();
                await db.DisposeAsync();
                if(auction is not null && channel is not null)
                {
                    if (auction.BidderId != 0)
                    {
                        var winner = await _client.GetUserAsync(auction.BidderId);
                        if (winner is not null)
                        {
                            var eb = new DiscordEmbedBuilder()
                            .WithTitle("Auction Won!")
                            .WithDescription($"{winner.Mention} has won {auction.Name} for {auction.CurrentBid}!")
                            .WithFooter($"ID: {auction.Id}");
                            await channel.SendMessageAsync(eb);
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
    }
}
