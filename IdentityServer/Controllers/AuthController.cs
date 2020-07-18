using IdentityServer.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer.Controllers
{
	public class AuthController : Controller
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly IIdentityServerInteractionService _interactionService;

		public AuthController(
			UserManager<IdentityUser> userManager,
			SignInManager<IdentityUser> signInManager,
			IIdentityServerInteractionService interactionService)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_interactionService = interactionService;
		}

		[HttpGet]
		public async Task<IActionResult> Logout(string logoutId)
		{
			await _signInManager.SignOutAsync();

			var logoutRequest = await _interactionService.GetLogoutContextAsync(logoutId);

			if (string.IsNullOrEmpty(logoutRequest.PostLogoutRedirectUri))
			{
				return RedirectToAction("Index", "Home");
			}

			return Redirect(logoutRequest.PostLogoutRedirectUri);
		}

		[HttpGet]
		public async Task<IActionResult> Login(string returnUrl)
		{
			var context = await _interactionService.GetAuthorizationContextAsync(returnUrl);
			string authenticationSchemeName = "cookies";	// default

			// Was a tenant id provided?
			if (context?.Tenant != null)
			{
				// Check what auth scheme is configured for this tenant.
				authenticationSchemeName = Configuration.GetTenantAuthenticationScheme(context.Tenant);
			}
			// Can fail auth here if tenant id is mandatory.
			//else
			//{
			//throw Exception(); or respond accordingly
			//}

			// Get available external providers.
			var externalProviders = await _signInManager.GetExternalAuthenticationSchemesAsync();

			if (authenticationSchemeName != "cookies")
			{
				// Skip the Login page, redirect to external provider action.
				var authenticationScheme = externalProviders.First(p => p.Name == authenticationSchemeName);
				return RedirectToAction("ExternalLogin", new { provider = authenticationScheme.Name, returnUrl });
			}

			return View(new LoginViewModel
			{
				ReturnUrl = returnUrl,
				ExternalProviders = externalProviders
			});
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel vm)
		{
			// check if the model is valied (but were not doign that now for brevity)

			// sign in
			var result = await _signInManager.PasswordSignInAsync(vm.Username, vm.Password, false, false);

			if (result.Succeeded)
			{
				return Redirect(vm.ReturnUrl);
			}
			else if (result.IsLockedOut)
			{
				// do something
			}

			vm.Password = null;

			return View(vm);
		}

		[HttpGet]
		public IActionResult Register(string returnUrl)
		{
			return View(new RegisterViewModel { ReturnUrl = returnUrl });
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterViewModel vm)
		{
			if (!ModelState.IsValid)
			{
				return View(vm);
			}

			var user = new IdentityUser(vm.Username);
			var result = await _userManager.CreateAsync(user, "password");

			if (result.Succeeded)
			{
				await _signInManager.SignInAsync(user, false);
				return Redirect(vm.ReturnUrl);
			}

			return View();
		}

		public IActionResult ExternalLogin(string provider, string returnUrl = null)
		{
			// Request a redirect to the external login provider.
			var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
			var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			return Challenge(properties, provider); // goes to middlewhere (e.g. OIDC, Facebook, Google, etc..) 
		}

		public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
		{
			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				return RedirectToAction("Login");	// should supply some error message to login page.
			}

			var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
			// can we match the authenticated account on the external provider to the account here on the identity server.

			if (result.Succeeded)
			{
				return Redirect(returnUrl);
			}

			// Register the user.
			var username = info.Principal.FindFirst(ClaimTypes.Name).Value;
			return View("ExternalRegister", new ExternalRegisterViewModel
			{
				Username = username,
				ReturnUrl = returnUrl
			});
		}

		[HttpPost]
		public async Task<IActionResult> ExternalRegister(ExternalRegisterViewModel vm)
		{
			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				return RedirectToAction("Login");   // should supply some error message to login page.
			}

			var user = new IdentityUser(vm.Username);
			var result = await _userManager.CreateAsync(user);

			if (!result.Succeeded)
			{
				return View(vm);
			}

			// TODO: check if user already exists or something else went wrong.

			// Fuse external and interal users together.
			result = await _userManager.AddLoginAsync(user, info);

			if (!result.Succeeded)
			{
				return View(vm);
			}

			await _signInManager.SignInAsync(user, false);

			return Redirect(vm.ReturnUrl);
		}
	}
}
