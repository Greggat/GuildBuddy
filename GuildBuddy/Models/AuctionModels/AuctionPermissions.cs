using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Models.AuctionModels
{
    [Flags]
    public enum AuctionPermissions : short
    {
        None = 0,
        View = 1,
        Bid = 2,
        Create = 4,
        Remove = 8,
        All = short.MaxValue,   //0111 1111 1111 1111
        Banned = short.MinValue //1000 0000 0000 0000
    }
}
