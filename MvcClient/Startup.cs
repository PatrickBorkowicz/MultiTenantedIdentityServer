using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Tasks;

namespace AuthenticationOptionsSample
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
			services.AddControllersWithViews();

			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).
				AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
				{
					// Required for Safari 12 issue and OpenID Connect.
					options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
				}).
				AddOpenIdConnect("OpenIdConnect", options =>
				{
					options.Authority = "https://localhost:44327/";
					options.ClientId = "client_id_mvc";
					options.ClientSecret = "client_secret_mvc";
					options.SaveTokens = true;
					options.ResponseType = "code";
					options.SignedOutCallbackPath = "/Home/Index";
				});

			services.AddMultiTenant().
				WithConfigurationStore().
				WithRouteStrategy().
				WithRemoteAuthentication(). // Important!
				WithPerTenantOptions<AuthenticationOptions>((options, tenantInfo) =>
				{
					// Allow each tenant to have a different default challenge scheme.
					if (tenantInfo.Items.TryGetValue("ChallengeScheme", out object challengeScheme))
					{
						options.DefaultChallengeScheme = (string)challengeScheme;
						// options.DefaultSignOutScheme = (string)challengeScheme;
					}
				}).
				WithPerTenantOptions<CookieAuthenticationOptions>((options, tenantInfo) =>
				{
					// Set a unique cookie name for this tenant.
					options.Cookie.Name = tenantInfo.Id + "-cookie";

					// Note the paths set take our routing strategy into account.
					options.LoginPath = "/" + tenantInfo.Identifier + "/Home/Login";
					options.Cookie.Path = "/" + tenantInfo.Identifier;
				}).
				WithPerTenantOptions<OpenIdConnectOptions>((options, tenantInfo) =>
				{
					options.Events = new OpenIdConnectEvents
					{
						OnRedirectToIdentityProvider = context =>
						{
							// Pass Tenant Id to IdentityServer.
							context.ProtocolMessage.AcrValues = $"tenant:{tenantInfo.Id}";
							return Task.CompletedTask;
						}
					};
				});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.EnvironmentName == "Development")
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseStaticFiles();
			app.UseRouting();
			app.UseMultiTenant();
			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute("default", "{__tenant__=}/{controller=Home}/{action=Index}");
			});
		}
	}
}