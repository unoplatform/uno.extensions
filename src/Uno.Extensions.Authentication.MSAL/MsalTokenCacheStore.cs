// Deliberately free of #if branches, of Uno dependencies and of Microsoft.Identity.Client types:
// Uno.Extensions.Authentication.MSAL.Tests compiles this file as linked source (the WinUI assembly
// can't load in a plain test host), and MSAL's TokenCacheNotificationArgs can't be constructed by a
// test. Keeping the byte[] <-> storage half here is the seam that makes the browser cache testable;
// MsalAuthenticationProvider owns the untestable half - registering the SetBeforeAccessAsync /
// SetAfterAccessAsync callbacks that call into it.
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
// Microsoft.Identity.Client (a global using in the MSAL project) also defines LogLevel;
// MsalAuthenticationProvider disambiguates the same way.
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Uno.Extensions.Storage.KeyValueStorage;

namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Reads and writes the serialized MSAL token cache (the <c>SerializeMsalV3</c> blob) through an
/// <see cref="IKeyValueStorage"/>. Used on WebAssembly, where <c>MsalCacheHelper</c> has no
/// backend - see specs/011-wasm-msal-token-cache/spec.md.
/// </summary>
internal sealed class MsalTokenCacheStore
{
	/// <summary>
	/// Prefix for the stored cache entry.
	/// </summary>
	/// <remarks>
	/// This is a persisted-storage contract: changing it orphans existing caches, signing every
	/// user out once.
	/// <para>
	/// It MUST NOT begin with <c>Uno.Extensions.Authentication.TokenCache</c>'s <c>AuthToken_</c>
	/// prefix. That type returns <em>every</em> key matching the prefix from
	/// <c>GetAsync</c> and hands the result to the HTTP handlers as a map of tokens, so a
	/// colliding key would be injected into outgoing requests as if it were a bearer token.
	/// <c>Given_MsalTokenCacheStore.When_Key_Does_Not_Collide_With_TokenCache_Prefix</c> guards this.
	/// </para>
	/// </remarks>
	internal const string KeyPrefix = "MsalCache_";

	/// <summary>
	/// Used when no client id is available, so the key stays well-formed rather than collapsing to
	/// the bare prefix.
	/// </summary>
	internal const string DefaultKeySuffix = "default";

	private readonly IKeyValueStorage _storage;
	private readonly ILogger _logger;

	/// <summary>
	/// Creates a store for the cache belonging to <paramref name="clientId"/>.
	/// </summary>
	/// <param name="storage">The key-value storage to read and write through.</param>
	/// <param name="logger">Logger for diagnostics. Never receives cache content.</param>
	/// <param name="clientId">
	/// The MSAL client id, which scopes the entry so caches for different app registrations stay
	/// apart while apps sharing a registration share sign-in state.
	/// </param>
	public MsalTokenCacheStore(IKeyValueStorage storage, ILogger logger, string? clientId)
	{
		_storage = storage;
		_logger = logger;
		Key = GetKey(clientId);
	}

	/// <summary>
	/// The storage key this instance reads and writes.
	/// </summary>
	public string Key { get; }

	/// <summary>
	/// Computes the storage key for a client id.
	/// </summary>
	internal static string GetKey(string? clientId) =>
		clientId is { Length: > 0 }
			? $"{KeyPrefix}{clientId}"
			: $"{KeyPrefix}{DefaultKeySuffix}";

	/// <summary>
	/// Loads the serialized cache, or <c>null</c> when nothing is stored or the stored value can't
	/// be read back.
	/// </summary>
	public async ValueTask<byte[]?> LoadAsync(CancellationToken ct)
	{
		var encoded = await _storage.GetStringAsync(Key, ct);
		if (encoded is not { Length: > 0 })
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"No stored MSAL token cache under '{Key}'");
			return null;
		}

		try
		{
			var blob = Convert.FromBase64String(encoded);
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Loaded MSAL token cache from '{Key}' ({blob.Length} bytes)");
			return blob;
		}
		catch (FormatException ex)
		{
			// Browser storage is editable by the user and by any script on the origin, so a
			// malformed value is reachable without a bug on our side. Dropping it costs a sign-in;
			// letting it reach DeserializeMsalV3 would throw on every token operation instead.
			if (_logger.IsEnabled(LogLevel.Warning)) _logger.LogWarningMessage($"Stored MSAL token cache under '{Key}' isn't valid base64 and has been discarded - the user will be asked to sign in again [{ex.GetType().Name}]");
			await ClearAsync(ct);
			return null;
		}
	}

	/// <summary>
	/// Persists the serialized cache. An empty or <c>null</c> blob clears the entry rather than
	/// storing a placeholder, which is what MSAL produces once the last account is removed.
	/// </summary>
	public async ValueTask SaveAsync(byte[]? blob, CancellationToken ct)
	{
		if (blob is not { Length: > 0 })
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"MSAL token cache is empty; clearing '{Key}'");
			await ClearAsync(ct);
			return;
		}

		// Length only, never content: every byte here is token material (AGENTS.md section 7).
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Saving MSAL token cache to '{Key}' ({blob.Length} bytes)");
		await _storage.SetAsync(Key, Convert.ToBase64String(blob), ct);
	}

	/// <summary>
	/// Removes the stored cache. Safe to call when nothing is stored.
	/// </summary>
	public async ValueTask ClearAsync(CancellationToken ct)
	{
		if (!await _storage.ContainsKey(Key, ct))
		{
			return;
		}

		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Clearing MSAL token cache '{Key}'");
		await _storage.ClearAsync(Key, ct);
	}
}
