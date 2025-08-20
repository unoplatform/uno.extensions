namespace Uno.Extensions.Authentication;

/// <summary>
/// Provides a builder for configuring and creating instances of <see cref="TokenCacheOptions"/>.
/// </summary>
/// <remarks>
/// This class allows for a fluent API to configure token cache options, such as specifying keys for
/// access tokens, refresh tokens, ID tokens, and other custom token keys.<br/>
/// </remarks>
public class TokenCacheOptionsBuilder
{
	private readonly TokenCacheOptions _preConfiguredOptions;
	private string? _accessTokenKey;
	private string? _refreshTokenKey;
	private string? _idTokenKey;
	private IDictionary<string, string> _otherTokenKeys = new Dictionary<string,string>();
	/// <summary>
	/// Creates a new instance of the <see cref="TokenCacheOptionsBuilder"/> class, optionally initializing it with
	/// existing token cache options.
	/// </summary>
	/// <param name="existingTokenCacheOptions">
	/// optional: A <see cref="TokenCacheOptions"/> instance to initialize the builder with. If <see langword="null"/>, the builder will be
	/// initialized with a new options object.</param>
	/// <returns>
	/// A new <see cref="TokenCacheOptionsBuilder"/> instance configured with the specified or default token cache options.
	/// </returns>
	public static TokenCacheOptionsBuilder Create(TokenCacheOptions? existingTokenCacheOptions = null)
		=> new TokenCacheOptionsBuilder(existingTokenCacheOptions);
	private TokenCacheOptionsBuilder(TokenCacheOptions? existingTokenCacheOptions = null)
	{
		_preConfiguredOptions = existingTokenCacheOptions ?? new ();
	}
	/// <summary>
	/// Sets the key used to cache access tokens.
	/// </summary>
	/// <param name="key">
	/// The key to associate with cached access tokens.
	/// </param>
	/// <returns>
	/// The current <see cref="TokenCacheOptionsBuilder"/> instance for method chaining.
	/// </returns>
	public TokenCacheOptionsBuilder AccessTokenKey(string key)
	{
		_accessTokenKey = key;
		return this;
	}
	/// <summary>
	/// Sets the key used to store the refresh token in the token cache.
	/// </summary>
	/// <param name="key">
	/// The key to associate with the refresh token.</param>
	/// <returns>
	/// The current <see cref="TokenCacheOptionsBuilder"/> instance, allowing for method chaining.
	/// </returns>
	public TokenCacheOptionsBuilder RefreshTokenKey(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			_refreshTokenKey = key;
		}
		return this;
	}
	/// <summary>
	/// Sets the key used to store the ID token in the token cache.
	/// </summary>
	/// <param name="key">
	/// The key to associate with the ID token.
	/// </param>
	/// <returns>
	/// The current <see cref="TokenCacheOptionsBuilder"/> instance, allowing for method chaining.
	/// </returns>
	public TokenCacheOptionsBuilder IdTokenKey(string key)
	{
		if(!string.IsNullOrWhiteSpace(key))
		{
			_idTokenKey = key;
		}
		return this;
	}
	/// <summary>
	/// Adds a mapping between a token code key and a URL key to the token cache options.
	/// </summary>
	/// <remarks>
	/// If either <paramref name="codeKey"/> or <paramref name="urlKey"/> is null, empty, or consists only
	/// of whitespace, the entry will not be added.
	/// </remarks>
	/// <param name="codeKey">
	/// The key representing the token in code, e.g. 'UserTokenKey'. Must not be null, empty, or whitespace.</param>
	/// <param name="urlKey">The key representing the associated URL formatted name to be found in x-www-form-urlencoded http responses. Must not be null, empty, or whitespace.</param>
	/// <returns>
	/// The current <see cref="TokenCacheOptionsBuilder"/> instance, allowing for method chaining.
	/// </returns>
	public TokenCacheOptionsBuilder AddOtherTokenKey(string codeKey, string urlKey)
	{
		if(!string.IsNullOrWhiteSpace(codeKey) && !string.IsNullOrWhiteSpace(urlKey))
		{
			_otherTokenKeys[codeKey] = urlKey;
		}
		return this;
	}
	/// <summary>
	/// Builds and returns a configured <see cref="TokenCacheOptions"/> instance.
	/// </summary>
	/// <remarks>
	/// This method consolidates the current configuration and pre-configured options to produce a <see cref="TokenCacheOptions"/> object.
	/// If specific token keys (e.g., access token, refresh token, or ID token) are not
	/// explicitly set, the corresponding values from the pre-configured options will be used.
	/// Additionally, any other token keys are merged and updated as necessary.</remarks>
	/// <returns>
	/// A <see cref="TokenCacheOptions"/> instance containing the configured token keys and other token-related settings.
	/// </returns>
	internal TokenCacheOptions Build()
	{
		foreach (var (codeKey, urlKey) in _otherTokenKeys)
		{
		    _preConfiguredOptions.OtherTokenKeys.AddOrReplace(codeKey, urlKey);
		}

		var options = new TokenCacheOptions
		{
			AccessTokenKey = _accessTokenKey ?? _preConfiguredOptions.AccessTokenKey,
			RefreshTokenKey = _refreshTokenKey ?? _preConfiguredOptions.RefreshTokenKey,
			IdTokenKey = _idTokenKey ?? _preConfiguredOptions.IdTokenKey,
			OtherTokenKeys = _otherTokenKeys
		};

		return options;

	}
}
