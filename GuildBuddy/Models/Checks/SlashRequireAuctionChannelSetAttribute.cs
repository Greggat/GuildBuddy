using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GuildBuddy.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GuildBuddy.Models.Checks
{
    public class SlashRequireAuctionChannelSetAttribute : SlashCheckBaseAttribute
    {
        public SlashRequireAuctionChannelSetAttribute()
        {
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            var service = ctx.Services.GetService<AuctionService>();
            var channelId = service.GetNotificationChannelIdFromCache(ctx.Guild.Id);

            if(channelId != 0)
            {
                var channel = ctx.Guild.GetChannel(channelId);
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
