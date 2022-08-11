using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace TestBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[AllowAnonymous]
public class CustomAuthController : ControllerBase
{

	private readonly ILogger _logger;

	public CustomAuthController(ILogger<CustomAuthController> logger)
	{
		_logger = logger;
	}

	[HttpGet(Name = "Login")]
	public AuthResponse Login(string username, string password)
	{
		var token = $"{username}:{password}".Base64Encode();
		_logger.LogTrace($"Token: {token}");
		return new AuthResponse(token);
	}

	[HttpPost(Name = "LoginCookie")]
	public void LoginCookie([FromQuery] string username, [FromQuery] string password)
	{
		var token = $"{username}:{password}".Base64Encode();
		_logger.LogTrace($"Token: {token}");

		Response.Cookies.Append("AccessToken", token, new CookieOptions
		{
			Secure = false, // For local non-https testing this needs to be false otherwise cookies can't be extracted from cookiecontainer
			HttpOnly = true,
			SameSite = SameSiteMode.None
		});
		Response.Cookies.Append("RefreshToken", "Refresh-" + token, new CookieOptions
		{
			Secure = false, // For local non-https testing this needs to be false otherwise cookies can't be extracted from cookiecontainer
			HttpOnly = true,
			SameSite = SameSiteMode.None
		});
	}

	[HttpPost(Name = "RefreshCookie")]
	public void RefreshCookie()
	{
		try
		{
			var token = Request.Cookies.FirstOrDefault(x => x.Key == "AccessToken").Value;
			var bits = token.Base64Decode().Split(":");
			if (bits.Length == 2)
			{
				Response.Cookies.Append("AccessToken", token, new CookieOptions
				{
					Secure = false, // For local non-https testing this needs to be false otherwise cookies can't be extracted from cookiecontainer
					HttpOnly = true,
					SameSite = SameSiteMode.None
				});
				Response.Cookies.Append("RefreshToken", $"Refresh-{DateTime.Now.ToString("HH:mm:sss")}" + token, new CookieOptions
				{
					Secure = false, // For local non-https testing this needs to be false otherwise cookies can't be extracted from cookiecontainer
					HttpOnly = true,
					SameSite = SameSiteMode.None
				});
				Response.StatusCode = StatusCodes.Status200OK;
				return;
			}
		}
		catch
		{

		}
		Response.StatusCode = StatusCodes.Status401Unauthorized;		
	}

	[HttpGet(Name = "GetDataAuthorizationHeader")]
	public IEnumerable<string>? GetDataAuthorizationHeader()
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
					try
					{
						var bits = tokenBits[1].Base64Decode().Split(":");
						if (bits.Length == 2)
						{
							return new[] { "One", "Two", "Three" };
						}
					}
					catch
					{
						var jwtHandler = new JwtSecurityTokenHandler();
						var jwt = jwtHandler.ReadJwtToken(tokenBits[1]);
						if (jwt is not null)
						{
							return new[] { "Four", "Five", "Six" };
						}

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



	[HttpGet(Name = "GetDataCookie")]
	public IEnumerable<string>? GetDataCookie()
	{
		try
		{
			var token = Request.Cookies.FirstOrDefault(x => x.Key == "AccessToken").Value;
			var bits = token.Base64Decode().Split(":");
			if (bits.Length == 2)
			{
				return new[] { "One", "Two", "Three" };
			}
			Response.StatusCode = StatusCodes.Status401Unauthorized;
			return default;
		}
		catch
		{

		}
		Response.StatusCode = StatusCodes.Status401Unauthorized;
		return default;
	}
}


public record AuthResponse(string AccessToken);

public static class AuthExtensions
{

	public static string Base64Encode(this string plainText)
	{
		var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
		return System.Convert.ToBase64String(plainTextBytes);
	}
	public static string Base64Decode(this string base64EncodedData)
	{
		var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
		return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
	}
}
