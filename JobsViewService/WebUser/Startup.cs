using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebUser.Startup))]
namespace WebUser
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
