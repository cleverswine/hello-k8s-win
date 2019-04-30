using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Surescripts.WebUI.Hubs;
using Surescripts.WebUI.Services;

namespace Surescripts.WebUI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<IMQClient, MQClient>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/liveness", lapp => lapp.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Body.WriteAsync(Encoding.Default.GetBytes("I am alive!\n"));
            }));
            app.Map("/readiness", lapp => lapp.Run(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Body.WriteAsync(Encoding.Default.GetBytes("I am ready!\n"));
            }));

            app.UseStaticFiles();
            app.UseSignalR(routes =>
            {
                routes.MapHub<StatusHub>("/statusHub");
            });
            app.UseMvc();
        }
    }
}
