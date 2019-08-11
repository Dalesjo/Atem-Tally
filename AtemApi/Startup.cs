using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AtemApi.Hubs;
using AtemApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtemApi
{


    public class Startup
    {
        private readonly ILogger log;
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            log = logger;
            log.LogInformation("Startup is started");



        }

        public static Dictionary<string, object> MailSettings { get; private set; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseCors("DalesjoCorsPolicy");

            app.UseSignalR(routes =>
            {
                routes.MapHub<TallyHub>("/tally");

            });

            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSignalR();

            services.AddHostedService<AtemService>();
            SetCors(services);
        }

        /// <summary>
        /// Set [EnableCors("DalesjoCorsPolicy")] on the controller or function to enable cors.
        /// </summary>
        /// <param name="services"></param>
        private void SetCors(IServiceCollection services)
        {
            var sectionOrgin = Configuration.GetSection("Cors:Orgin");
            var orgins = sectionOrgin.Get<string[]>();

            var sectionHeader = Configuration.GetSection("Cors:Headers");
            var headers = sectionHeader.Get<string[]>();

            var sectionAge = Configuration.GetSection("Cors:MaxAge");
            var Age = sectionAge.Get<int>();

            services.AddCors(o => o.AddPolicy("DalesjoCorsPolicy", builder =>
            {
                builder.WithOrigins(orgins)
                       .AllowAnyMethod()
                       .WithHeaders(headers)
                       .AllowCredentials()
                       .SetPreflightMaxAge(TimeSpan.FromSeconds(Age));
            }));
        }
    }
}
