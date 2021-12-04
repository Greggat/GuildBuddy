using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Data
{
    public class GuildBuddyContext : DbContext
    {
        public DbSet<Auction> Auctions { get; set; }
        public string DbPath { get; }

        public GuildBuddyContext()
        {
            //var folder = Environment.SpecialFolder.LocalApplicationData;
            //var path = Environment.GetFolderPath(folder);
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            DbPath = Path.Join(path, "guildbuddy.db");

            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }

}
