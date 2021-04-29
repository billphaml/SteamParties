using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SteamParties.Startup))]
namespace SteamParties
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
