

using System.Threading;
using Uno.Extensions.Serialization;

namespace Uno.Extensions.Authentication;

/// <summary>
/// Extension methods for <see cref="ITokenCache"/>.
/// </summary>
public static class TokenCacheExtensions
{
	/// <summary>
	/// Defines a key for the token cache which corresponds to an access token element.
	/// </summary>
	public const string AccessTokenKey = "AccessToken";

	/// <summary>
	/// Defines a key for the token cache which corresponds to a refresh token element.
	/// </summary>
	public const string RefreshTokenKey = "RefreshToken";

	/// <summary>
	/// Gets the access token from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="cancellation">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents an asynchronous operation. The task result is the access token or null.
	/// </returns>
	public static ValueTask<string> AccessTokenAsync(this ITokenCache cache, CancellationToken? cancellation = default)
	{
		return cache.TokenAsync(AccessTokenKey, cancellation);
	}

	/// <summary>
	/// Gets the refresh token from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="cancellation">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents an asynchronous operation. The task result is the refresh token or null.
	/// </returns>
	public static ValueTask<string> RefreshTokenAsync(this ITokenCache cache, CancellationToken? cancellation = default)
	{
		return cache.TokenAsync(RefreshTokenKey, cancellation);
	}

	/// <summary>
	/// Gets a token which is represented by a specified key from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="tokenKey">
	/// The key of the token to get.
	/// </param>
	/// <param name="cancellation">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents an asynchronous operation. The task result is the token or null.
	/// </returns>
	public static async ValueTask<string> TokenAsync(this ITokenCache cache, string tokenKey, CancellationToken? cancellation = default)
	{
		var tokens = await cache.GetAsync(cancellation ?? CancellationToken.None);
		return tokens.FirstOrDefault(x => x.Key == tokenKey).Value;
	}

	/// <summary>
	/// Saves the values provided for access and refresh tokens in the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="provider">
	/// The name of the authentication provider for which the tokens will be saved.
	/// </param>
	/// <param name="accessToken">
	/// The access token to save. Optional
	/// </param>
	/// <param name="refreshToken">
	/// The refresh token to save. Optional
	/// </param>
	/// <param name="cancellation">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents an asynchronous operation.
	/// </returns>
	public static async ValueTask SaveTokensAsync(this ITokenCache cache, string provider, string? accessToken = null, string? refreshToken = null, CancellationToken? cancellation = default)
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

	/// <summary>
	/// Gets a typed entity represented by the token corresponding to a specified key from the provided dictionary.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to get. It should be that of the entity which is represented by the serialized token.
	/// </typeparam>
	/// <param name="tokens">
	/// The dictionary of tokens to use.
	/// </param>
	/// <param name="serializer">
	/// The serializer to use.
	/// </param>
	/// <param name="key">
	/// The key of the token to get.
	/// </param>
	/// <returns>
	/// The entity or null if the token is not found or is empty.
	/// </returns>
	public static TEntity? Get<TEntity>(this IDictionary<string, string> tokens, ISerializer<TEntity> serializer, string key)
	{
		if (tokens.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
		{
			return serializer.FromString(value);
		}
		return default;
	}

	/// <summary>
	/// Serializes the entity provided and sets the result as the value of a token 
	/// element corresponding to a specified key in the dictionary.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to set. It should be what the serialized token will represent.
	/// </typeparam>
	/// <param name="tokens">
	/// The dictionary of tokens to use.
	/// </param>
	/// <param name="serializer">
	/// The serializer to use.
	/// </param>
	/// <param name="key">
	/// The key of the token to set.
	/// </param>
	/// <param name="entity">
	/// The entity to serialize and set.
	/// </param>
	public static void Set<TEntity>(this IDictionary<string, string> tokens, ISerializer<TEntity> serializer, string key, TEntity entity)
	{
		tokens[key] = serializer.ToString(entity);
	}
}
