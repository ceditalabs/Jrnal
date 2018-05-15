using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Cedita.Labs.Jrnal.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MvcOptions>(o =>
            {
                var requireAuthenticatedUser = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                o.Filters.Add(new AuthorizeFilter(requireAuthenticatedUser));
            });

            var authConfig = new Models.Configuration.Authentication();
            Configuration.GetSection("Authentication").Bind(authConfig);

            services.AddAuthentication(o =>
            {
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(o =>
            {
                o.Authority = authConfig.OpenIdAuthority;
                o.ClientId = authConfig.OpenIdClientId;
                o.ClientSecret = authConfig.OpenIdClientSecret;
                o.ResponseType = "code id_token";
            });

            services.AddAuthorization(o =>
            {
                o.AddPolicy("RequireSid", policy => policy.RequireAssertion(ctx =>
                {
                    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userId != null)
                    {
                        return authConfig.AllowedSids.Contains(userId.Value);
                    }
                    return false;
                }));

                o.DefaultPolicy = o.GetPolicy("RequireSid");
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
