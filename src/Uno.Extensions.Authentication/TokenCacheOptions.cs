namespace Uno.Extensions.Authentication;
/// <summary>
/// Provides predefined keys for identifying elements in the <see cref="ITokenCache"/>
/// </summary>
/// <remarks>
/// These keys are used to store and retrieve specific token elements such as access tokens, refresh tokens, ID tokens, and expiration times.<br/>
/// They are also used in the <see cref="TokenCacheExtensions"/> as default keys for the corresponding token elements.
/// </remarks>
/// <example>
/// You can use to define a json object in your appsettings file like this to provide your own token cache keys:
/// <code>
/// {
///   "TokenOptions": {
///     "AccessTokenKey": "my_custom_access_token_key",
///     "RefreshTokenKey": "my_custom_refresh_token_key",
///     "IdTokenKey": "my_custom_id_token_key",
///     "other_token_keys": {
///       "ExpiresInKey": "expires_in",
///       "StateKey": "state",
///       "CodeKey": "code"
///     }
///   }
/// }
/// </code>
/// </example>
public record TokenCacheOptions
{
	/// <summary>
	/// Defines a key for the token cache which corresponds to a access token.
	/// </summary>
	public string AccessTokenKey { get; init; } = "AccessToken";

	/// <summary>
	/// Gets the cache key used to store the refresh token.
	/// </summary>
	public string RefreshTokenKey { get; init; } = "RefreshToken";
	/// <summary>
	/// Defines a key for the token cache which corresponds to an ID token element.
	/// </summary>
	public string IdTokenKey { get; init; } = "IdToken";
	/// <summary>
	/// Defines a key for the token cache which corresponds to an expiration time element.
	/// </summary>
	public string ExpiresInKey { get; init; } = "ExpiresIn";
}
