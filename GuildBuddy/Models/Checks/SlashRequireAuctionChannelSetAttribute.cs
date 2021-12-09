using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GuildBuddy.Models.AuctionModels;
using GuildBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Models.Checks
{
    public class SlashRequireAuctionChannelSetAttribute : SlashCheckBaseAttribute
    {
        public SlashRequireAuctionChannelSetAttribute()
        {
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            //TODO: Get ChannelId from AuctionService if possible?...
            using var db = new GuildBuddyContext();
            var auctionChannel = db.AuctionChannels.Where(o => o.GuildId == ctx.Guild.Id).FirstOrDefault();

            if(auctionChannel is not null)
            {
                var channel = ctx.Guild.GetChannel(auctionChannel.ChannelId);
                return channel is not null;
            }
            var eb = new DiscordEmbedBuilder()
                .WithTitle("Auction Channel Not Set")
                .WithDescription("In order to use auction features please have an admin set the auction notifications channel" +
                "by using the `/auction setchannel` command in the channel you wish to receieve auction notifications");
            await ctx.Channel.SendMessageAsync(eb);
            return false;
        }
    }
}
