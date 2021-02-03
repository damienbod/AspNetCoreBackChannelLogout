using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using MvcHybridBackChannelTwo.BackChannelLogout;

namespace MvcHybridBackChannelTwo
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        private IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            Configuration = configuration;
            _environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<CookieEventHandler>();
            services.AddSingleton<LogoutSessionManager>();
            services.AddHttpClient();

            services.Configure<AuthConfiguration>(Configuration.GetSection("AuthConfiguration"));

            var authConfiguration = Configuration.GetSection("AuthConfiguration");
            var clientId_aud = authConfiguration["Audience"];

            var redisConnectionString = Configuration.GetConnectionString("RedisCacheConnection");

            if (string.IsNullOrEmpty(redisConnectionString))
            {
                // remove this, if your use a proper development cache which uses the same as the production
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddDistributedRedisCache(options =>
                {
                    options.Configuration = Configuration.GetConnectionString("RedisCacheConnection");
                    options.InstanceName = "MvcHybridBackChannelTwoBackChannelTwoInstance";
                });
            }

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "oidc";
            })
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.Cookie.Name = "MvcHybridBackChannelTwobc";

                    options.EventsType = typeof(CookieEventHandler);
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = authConfiguration["StsServerIdentityUrl"];
                    options.RequireHttpsMetadata = false;

                    options.ClientSecret = Configuration["ClientSecret"]; ;
                    options.ClientId = clientId_aud;

                    options.ResponseType = "code id_token";
                    options.UsePkce = false;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("offline_access");

                    options.ClaimActions.Remove("amr");
                    options.ClaimActions.MapJsonKey("website", "website");

                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Name,
                        RoleClaimType = JwtClaimTypes.Role,
                    };
                });

            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            IdentityModelEventSource.ShowPII = true;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
