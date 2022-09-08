

using System.Threading;
using Uno.Extensions.Serialization;

namespace Uno.Extensions.Authentication;

public static class TokenCacheExtensions
{
	public const string AccessTokenKey = "AccessToken";
	public const string RefreshTokenKey = "RefreshToken";

	public static ValueTask<string> AccessTokenAsync(this ITokenCache cache, CancellationToken? cancellation = default)
	{
		return cache.TokenAsync(AccessTokenKey, cancellation);
	}

	public static ValueTask<string> RefreshTokenAsync(this ITokenCache cache, CancellationToken? cancellation = default)
	{
		return cache.TokenAsync(RefreshTokenKey, cancellation);
	}

	public static async ValueTask<string> TokenAsync(this ITokenCache cache, string tokenKey, CancellationToken? cancellation = default)
	{
		var tokens = await cache.GetAsync(cancellation ?? CancellationToken.None);
		return tokens.FirstOrDefault(x => x.Key == tokenKey).Value;
	}
	public static async ValueTask SaveTokensAsync(this ITokenCache cache, string provider, string? accessToken=null, string? refreshToken=null, CancellationToken? cancellation = default)
	{
		var ct = cancellation ?? CancellationToken.None;
		var dict = await cache.GetAsync(ct);
		if (!string.IsNullOrWhiteSpace(accessToken))
		{
			dict[AccessTokenKey] = accessToken!;
		}
		if (!string.IsNullOrWhiteSpace(refreshToken))
		{
			dict[RefreshTokenKey] = refreshToken!;
		}
		await cache.SaveAsync(provider, dict, ct);
	}

	public static TEntity? Get<TEntity>(this IDictionary<string, string> tokens, ISerializer<TEntity> serializer, string key)
	{
		if (tokens.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
		{
			return serializer.FromString(value);
		}
		return default;
	}

	public static void Set<TEntity>(this IDictionary<string, string> tokens, ISerializer<TEntity> serializer, string key, TEntity entity)
	{
		tokens[key] = serializer.ToString(entity);
	}
}
