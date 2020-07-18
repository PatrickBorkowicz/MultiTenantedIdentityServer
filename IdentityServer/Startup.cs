using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Data;
using Microsoft.IdentityModel.Tokens;

namespace IdentityServer
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<AppDbContext>(options =>
			{
				options.UseInMemoryDatabase("Memory");
			});

			services.AddIdentity<IdentityUser, IdentityRole>(options =>
			{
				options.Password.RequiredLength = 4;
				options.Password.RequireDigit = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
			})
				.AddEntityFrameworkStores<AppDbContext>()
				.AddDefaultTokenProviders();

			services.ConfigureApplicationCookie(options =>
			{
				options.Cookie.Name = "IdentityServer.Cookie";
				options.LoginPath = "/Auth/Login";
				options.LogoutPath = "/Auth/Logout";
			});

			services.AddAuthentication()
				 .AddOpenIdConnect("aad", "Login with Azure AD", options =>
				 {
					 options.Authority = $"https://login.microsoftonline.com/common";
					 options.TokenValidationParameters =
							  new TokenValidationParameters { ValidateIssuer = false };
					 options.ClientId = "xxx";
					 options.CallbackPath = "/signin-oidc";
				 });

			var assembly = typeof(Startup).Assembly.GetName().Name;

			services.AddIdentityServer()
				.AddAspNetIdentity<IdentityUser>()
				.AddInMemoryIdentityResources(Configuration.GetIdentityResources())
				.AddInMemoryApiResources(Configuration.GetApis())
				.AddInMemoryClients(Configuration.GetClients())
				.AddDeveloperSigningCredential();

			services.AddControllersWithViews().AddRazorRuntimeCompilation();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseIdentityServer();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapDefaultControllerRoute();
			});
		}
	}
}
