﻿using AspNetCoreRateLimit;
using GraphQL.Server;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedditSharp;

namespace MemeEconomy.Insights
{
    public partial class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var reddit = 
                new Reddit(
                    new BotWebAgent(
                        _config["reddit:username"],
                        _config["reddit:password"],
                        _config["reddit:clientId"],
                        _config["reddit:clientSecret"],
                        _config["reddit:redirectUri"]),
                    false);

            services.AddSingleton(reddit);

            services.AddHttpContextAccessor();

            ConfigureGraph(services);

            // Mostly rate-limiting stuff below here
            services.AddOptions();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(_config.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(_config.GetSection("IpRateLimitPolicies"));

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIpRateLimiting();

            app.UseWebSockets();

            app.UseGraphQL<Schema>("/graph");
            app.UseGraphQLWebSockets<Schema>("/graph");
        }
    }
}