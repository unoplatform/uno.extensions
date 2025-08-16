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
	public const string AccessTokenKey = "access_token";

	/// <summary>
	/// Defines a key for the token cache which corresponds to a refresh token element.
	/// </summary>
	public const string RefreshTokenKey = "refresh_token";

	/// <summary>
	/// Defines a key for the token cache which corresponds to an ID token element.
	/// </summary>
	public const string IdTokenKey = "id_token";

	/// <summary>
	/// Defines a key for the token cache which corresponds to an expires in element.
	/// </summary>
	public const string ExpiresInKey = "expires_in";
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
	/// Gets the ID token from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="idTokenKey">
	/// optional: The key of the ID token to get. If not provided, the default key <see cref="IdTokenKey"/> will be used.
	/// </param>
	/// <param name="cancellation">
	/// optional: A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> that represents an asynchronous operation. The task result is the access token value or <see langword="null"/>.
	/// </returns>
	public static ValueTask<string> IdTokenAsync(this ITokenCache cache, string idTokenKey, CancellationToken? cancellation = default)
	{
	    ArgumentNullException.ThrowIfNullOrWhiteSpace(idTokenKey, nameof(idTokenKey));
		return cache.TokenAsync(idTokenKey, cancellation);
	}

	/// <summary>
	/// Gets the access token from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="accessTokenKey">
	/// optional: The key of the access token to get. If not provided, the default key <see cref="AccessTokenKey"/> will be used.
	/// </param>
	/// <param name="cancellation">
	/// optional: A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> that represents an asynchronous operation. The task result is the access token value or <see langword="null"/>.
	/// </returns>
	public static ValueTask<string> AccessTokenAsync(this ITokenCache cache, string accessTokenKey, CancellationToken? cancellation = default)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(accessTokenKey, nameof(accessTokenKey));
		return cache.TokenAsync(accessTokenKey, cancellation);
	}

	/// <summary>
	/// Gets the refresh token from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="refreshTokenKey">
	/// optional: The key of the refresh token to get. If not provided, the default key <see cref="RefreshTokenKey"/> will be used.
	/// </param>
	/// <param name="cancellation">
	/// optional: A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> that represents an asynchronous operation. The task result is the refresh token value or <see langword="null"/>.
	/// </returns>
	public static ValueTask<string> RefreshTokenAsync(this ITokenCache cache, string refreshTokenKey, CancellationToken? cancellation = default)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(refreshTokenKey, nameof(refreshTokenKey));
		return cache.TokenAsync(refreshTokenKey, cancellation);
	}
	/// <summary>
	/// Gets the expiration time from the token cache.
	/// </summary>
	/// <param name="cache">
	/// The token cache from which to retrieve the expiration time.
	/// </param>
	/// <param name="expiresInKey">
	/// optional: The key used to identify the expiration time in the cache. If not provided, the default key <see cref="ExpiresInKey"/> will be used.</param>
	/// <param name="cancellation">
	/// optional: A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> that represents an asynchronous operation. The task result is the expires in token value or <see langword="null"/>.
	/// </returns>
	public static ValueTask<string> ExpiresInAsync(this ITokenCache cache, string expiresInKey, CancellationToken? cancellation = default)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(expiresInKey, nameof(expiresInKey));
		return cache.TokenAsync(expiresInKey, cancellation);
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
	/// optional: A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> that represents an asynchronous operation. The task result is the token or <see langword="null"/>.
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
	/// Saves the values provided for access and refresh tokens in the token cache.
	/// </summary>
	/// <param name="cache">
	/// The <see cref="ITokenCache"/> to use.
	/// </param>
	/// <param name="provider">
	/// The name of the authentication provider for which the tokens will be saved.
	/// </param>
	/// <param name="idTokenKey">
	/// optional: The key to use for the ID token. If not provided, defaults to <see cref="IdTokenKey"/>.
	/// </param>
	/// <param name="idToken">
	/// optional: The ID token to save.
	/// </param>
	/// <param name="accessTokenKey">
	/// optional: The key to use for the access token. If not provided, defaults to <see cref="AccessTokenKey"/>.
	/// </param>
	/// <param name="accessToken">
	/// optional: The access token to save.
	/// </param>
	/// <param name="refreshTokenKey">
	/// optional: The key to use for the refresh token. If not provided, defaults to <see cref="RefreshTokenKey"/>.
	/// </param>
	/// <param name="refreshToken">
	/// optional: The refresh token to save.
	/// </param>
	/// <param name="expiresInKey">
	/// optional: The key to use for the expires in value. If not provided, defaults to <see cref="ExpiresInKey"/>.
	/// </param>
	/// <param name="expiresIn">
	/// optional: The expires in value to save.
	/// </param>
	/// <param name="cancellation">
	/// optional: A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that represents an asynchronous operation.
	/// </returns>
	public static async ValueTask SaveNamedTokensAsync(this ITokenCache cache, string provider, string? idTokenKey = null, string? idToken = null, string? accessTokenKey = null, string? accessToken = null, string? refreshTokenKey = null, string? refreshToken = null, string? expiresInKey = null, string? expiresIn = null, CancellationToken? cancellation = default)
    {
        var ct = cancellation ?? CancellationToken.None;
        var dict = await cache.GetAsync(ct);
        if (!string.IsNullOrWhiteSpace(idToken))
        {
            dict[idTokenKey ?? IdTokenKey] = idToken!;
        }
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            dict[accessTokenKey ?? AccessTokenKey] = accessToken!;
        }
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            dict[refreshTokenKey ?? RefreshTokenKey] = refreshToken!;
        }
        if (!string.IsNullOrWhiteSpace(expiresIn))
        {
            dict[expiresInKey ?? ExpiresInKey] = expiresIn!;
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
	/// The Entity or <see langword="null"/> if the token is not found or is empty.
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
