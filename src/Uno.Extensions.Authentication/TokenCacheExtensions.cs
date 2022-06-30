

namespace Uno.Extensions.Authentication;

public static class TokenCacheExtensions
{
	public const string AccessTokenKey = "AccessToken";
	public const string RefreshTokenKey = "RefreshToken";

	public static ValueTask<string> AccessTokenAsync(this ITokenCache cache, CancellationToken? cancellation=default)
	{
		return cache.TokenAsync(AccessTokenKey, cancellation);
	}

	public static ValueTask<string> RefreshTokenAsync(this ITokenCache cache, CancellationToken? cancellation = default)
	{
		return cache.TokenAsync(RefreshTokenKey, cancellation);
	}

	public static async ValueTask<string> TokenAsync(this ITokenCache cache, string tokenKey, CancellationToken? cancellation = default)
	{
		var tokens = await cache.GetAsync(cancellation);
		return tokens.FirstOrDefault(x => x.Key == tokenKey).Value;
	}
	public static async ValueTask<bool> SaveTokensAsync(this ITokenCache cache, string provider, string? accessToken=null, string? refreshToken=null, CancellationToken? cancellation = default)
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
		return await cache.SaveAsync(provider, dict, cancellation);
	}
}
