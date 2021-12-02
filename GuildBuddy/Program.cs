using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace GuildBuddy
{
    class Program
    {
        public static Task Main(string[] args)
            => Startup.RunAsync(args);
    }
}