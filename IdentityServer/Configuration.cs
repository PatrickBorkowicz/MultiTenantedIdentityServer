using IdentityModel;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer
{
	public static class Configuration
	{
		public static IEnumerable<IdentityResource> GetIdentityResources() =>
			new List<IdentityResource>
			{
				new IdentityResources.OpenId(),
				new IdentityResources.Profile()
			};

		public static IEnumerable<ApiResource> GetApis() =>
			new List<ApiResource>
			{
				// e.g.
				new ApiResource("ApiOne"),
				new ApiResource("ApiTwo"),
			};

		public static IEnumerable<Client> GetClients() =>
			new List<Client>
			{
				new Client
				{
					ClientId = "client_id_mvc",
					ClientSecrets = { new Secret("client_secret_mvc".ToSha256()) },

					AllowedGrantTypes = GrantTypes.Code,

					RedirectUris = { "https://localhost:44307/signin-oidc" },
					PostLogoutRedirectUris = { "https://localhost:44307/Home/Index" },

					AllowedScopes =
					{
						"ApiOne",
						"ApiTwo",
						IdentityServer4.IdentityServerConstants.StandardScopes.OpenId,
						IdentityServer4.IdentityServerConstants.StandardScopes.Profile
					},

					RequireConsent = false, // can be enabled to have consent screen
				}
			};

		// Hardcoding this for now, but could be retrieved dynamically by implementing an IAuthenticationSchemeProvider 
		public static string GetTenantAuthenticationScheme(string tenantId)
		{
			switch (tenantId)
			{
				case "finbuckle":
					return "aad";
				default:
					return "cookies";
			}
		}
	}
}