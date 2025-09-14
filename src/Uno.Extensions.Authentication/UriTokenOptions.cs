namespace Uno.Extensions.Authentication;

/// <summary>
/// Provides predefined keys for identifying elements in the callback <see cref="Uri"/> for authentication responses.
/// </summary>
/// <remarks>
/// These keys are used to extract specific token elements such as access tokens, refresh tokens, ID tokens, and expiration times from the authentication callback Uri.<br/>
/// The default keys correspond to common OAuth2 and OpenID Connect parameter names.<br/>
/// For usage with JWT tokens, make sure to set the <see cref="IdTokenKey"/> to "id_token".<br/>
/// </remarks>
/// <example>
/// You can define a json object in your appsettings file like this to provide your own Uri token keys:
/// <code>
/// {
///   "UriTokenOptions": {
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
public record UriTokenOptions
{
	/// <summary>
	/// The default configuration section name used to retrieve token options.
	/// </summary>
	public const string DefaultSectionName = "UriTokenOptions";
	/// <summary>
	/// Defines a key for the parameter name to retrieve the access token element from the Callback Uri.
	/// </summary>
	public string AccessTokenKey { get; init; } = "access_token";
	/// <summary>
	/// Defines a key for the parameter name to retrieve the refresh token element from the Callback Uri.
	/// </summary>
	public string RefreshTokenKey { get; init; } = "refresh_token";
	/// <summary>
	/// Defines a key for the parameter name to retrieve the ID token element from the Callback Uri.
	/// </summary>
	public string IdTokenKey { get; init; } = "client_id";
	/// <summary>
	/// Defines a key for the parameter name to retrieve the expiration time element from the Callback Uri.
	/// </summary>
	public string ExpiresInKey { get; init; } = "expires_in";
	/// <summary>
	/// Defines a collection of other token keys that can be used to store additional tokens in the cache or retrieve them from the Authentication Response.
	/// </summary>
	/// <remarks>
	/// The Keys in this dictionary represent the <see cref="TokenCache"/> keys and the value represents the uri parameter names that are used to identify the token elements in the authentication response.
	/// </remarks>
	public IDictionary<string, string> OtherTokenKeys { get; init; } = new Dictionary<string, string>();
}
