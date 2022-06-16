

namespace Uno.Extensions.Authentication;

public static class TokenCacheExtensions
{
	internal const string AccessTokenKey = "AccessToken";
	internal const string RefreshTokenKey = "RefreshToken";

	public static async Task<string> AccessTokenAsync(this ITokenCache cache)
	{
		var tokens = await cache.GetAsync();
		return tokens.FirstOrDefault(x => x.Key == AccessTokenKey).Value;
	}

	public static async Task<string> RefreshTokenAsync(this ITokenCache cache)
	{
		var tokens = await cache.GetAsync();
		return tokens.FirstOrDefault(x => x.Key == RefreshTokenKey).Value;
	}

	public static async Task SaveTokensAsync(this ITokenCache cache, string? accessToken=null, string? refreshToken=null)
	{
		var dict = new Dictionary<string, string>();
		if (!string.IsNullOrWhiteSpace(accessToken))
		{
			dict[AccessTokenKey] = accessToken!;
		}
		if (!string.IsNullOrWhiteSpace(refreshToken))
		{
			dict[AccessTokenKey] = refreshToken!;
		}
		await cache.SaveAsync(dict);
	}
}
