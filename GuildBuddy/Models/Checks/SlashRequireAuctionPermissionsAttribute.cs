using DSharpPlus;
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
    public class SlashRequireAuctionPermissionsAttribute : SlashCheckBaseAttribute
    {
        private AuctionPermissions _requiredPerms;

        public SlashRequireAuctionPermissionsAttribute(AuctionPermissions requiredPerms)
        {
            _requiredPerms = requiredPerms;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            //TODO: Cache these permissions or load on startup
            if (ctx.Member.Permissions.HasFlag(Permissions.Administrator))
                return true;

            using var db = new GuildBuddyContext();
            var auctionPermissions = db.AuctionRoles.Where(o => o.GuildId == ctx.Guild.Id).ToDictionary(o => o.RoleId, o => o);

            AuctionPermissions perms = 0;
            
            foreach(var role in ctx.Member.Roles)
            {
                if(auctionPermissions.TryGetValue(role.Id, out var rolePerms))
                {
                    perms |= rolePerms.Permissions;
                }
            }

            return (perms & _requiredPerms) == _requiredPerms;
        }
    }
}
