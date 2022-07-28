using Microsoft.AspNetCore.Authentication;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace TestBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class WebAuthController : ControllerBase
{
	private readonly Auth authOptions;
	public WebAuthController(IOptions<Auth> options)
	{
		authOptions = options.Value;
	}

	[HttpGet("{scheme}")]
	public async Task Login([FromRoute] string scheme)
	{
		var auth = await Request.HttpContext.AuthenticateAsync(scheme);

		if (!auth.Succeeded
			|| auth?.Principal == null
			|| !auth.Principal.Identities.Any(id => id.IsAuthenticated)
			|| string.IsNullOrEmpty(auth.Properties.GetTokenValue("access_token")))
		{
			// Not authenticated, challenge
			await Request.HttpContext.ChallengeAsync(scheme);
		}
		else
		{
			var claims = auth.Principal.Identities.FirstOrDefault()?.Claims;
			var email = string.Empty;
			email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

			// Get parameters to send back to the callback
			var qs = new Dictionary<string, string>
				{
					{ "access_token", auth.Properties.GetTokenValue("access_token")?? string.Empty },
					{ "refresh_token", auth.Properties.GetTokenValue("refresh_token") ?? string.Empty },
					{ "expires_in", (auth.Properties.ExpiresUtc?.ToUnixTimeSeconds() ?? -1).ToString() },
					{ "email", email?? string.Empty }
				};

			var state = Request.Query["state"];
			if (state.Any())
			{
				qs["state"] = state.First();
			}

			// Build the result url
			var url = authOptions.CallbackScheme + "://callback?" + string.Join(
				"&",
				qs.Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "-1")
				.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

			// Redirect to final url
			Request.HttpContext.Response.Redirect(url);
		}
	}


	[HttpGet(Name = "GetDataFacebook")]
	public async Task<IEnumerable<string>?> GetDataFacebook()
	{
		try
		{
			var token = Request.Headers.Authorization.FirstOrDefault() ?? string.Empty;
			var tokenBits = token.Split(' ');
			if (tokenBits.Length == 2)
			{
				var scheme = tokenBits[0];
				if (scheme == "Bearer")
				{
					var fbToken = tokenBits[1];
					var tokenInfoUrl = $"https://graph.facebook.com/debug_token?input_token={fbToken}&access_token={fbToken}";
					var client = new HttpClient();
					var response = await client.GetAsync(tokenInfoUrl);
					if (response.IsSuccessStatusCode)
					{
						return new[] { "One", "Two", "Three" };
					}
				}
			}
		}
		catch
		{

		}
		Response.StatusCode = StatusCodes.Status401Unauthorized;
		return default;
	}
}
