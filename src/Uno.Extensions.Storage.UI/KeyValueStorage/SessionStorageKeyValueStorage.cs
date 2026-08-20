namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Persists values in the browser's <c>sessionStorage</c>: entries survive a page reload but are
/// dropped when the tab closes.
/// </summary>
/// <remarks>
/// WebAssembly only. <see cref="ApplicationDataKeyValueStorage"/> writes through
/// <c>ApplicationData.Current.LocalSettings</c>, which maps to <c>localStorage</c> in the browser,
/// and there is no WinRT surface for <c>sessionStorage</c> - hence the direct JS interop.
/// <para>
/// Selected as the default <see cref="IKeyValueStorage"/> on WebAssembly unless the browser cache
/// location says otherwise; see specs/011-wasm-msal-token-cache/spec.md.
/// </para>
/// </remarks>
internal record SessionStorageKeyValueStorage(
	ILogger<SessionStorageKeyValueStorage> Logger,
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings Settings,
	ISerializer Serializer
	) : BaseKeyValueStorageWithCaching(InMemoryStorage, Settings)
{
	public const string Name = "SessionStorage";

	// Do not change this value. It is a persisted-storage contract, and it is also what separates
	// our entries from everything else on the origin: sessionStorage is shared with the page's
	// other scripts, so both key enumeration and clear-all must stay inside this namespace.
	private const string KeyNameSuffix = "_SSKVS";

	/// <inheritdoc />
	public override bool IsEncrypted => false;

	/// <inheritdoc />
	protected override async ValueTask InternalClearAsync(string? name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Clearing value for key '{name}'.");
		}

		if (name is { Length: > 0 })
		{
			SessionStorageImports.RemoveItem(GetKey(name));
		}
		else
		{
			// Only our own keys, unlike ApplicationData's container-wide clear: sessionStorage
			// belongs to the whole origin, and the page's other scripts keep state there too.
			foreach (var key in await InternalGetKeysAsync(ct))
			{
				SessionStorageImports.RemoveItem(GetKey(key));
			}
		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}
	}

	/// <inheritdoc />
	protected override async ValueTask<string[]> InternalGetKeysAsync(CancellationToken ct)
	{
		// sessionStorage.length is a property, which [JSImport] cannot bind, so the end of the
		// store is detected the way the DOM spec defines it: key(index) is null past the last
		// entry. A handful of round-trips, and BaseKeyValueStorageWithCaching caches the result.
		var keys = new List<string>();
		for (var index = 0; ; index++)
		{
			var key = SessionStorageImports.Key(index);
			if (key is null)
			{
				break;
			}

			if (key.EndsWith(KeyNameSuffix, StringComparison.Ordinal))
			{
				keys.Add(GetName(key));
			}
		}

		return keys.ToArray();
	}

	/// <inheritdoc />
	protected override async ValueTask<T?> InternalGetAsync<T>(string name, CancellationToken ct)
		where T : default
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Getting value for key '{name}'.");
		}

		var data = SessionStorageImports.GetItem(GetKey(name));
		if (data is not { Length: > 0 })
		{
			// Absent, not broken: the base class treats null as a cache miss, and logging a
			// warning for every miss would drown the real ones.
			return default;
		}

		try
		{
			var value = Serializer.FromString<T>(data);

			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
			}

			return value;
		}
		catch (Exception ex)
		{
			// Anything on the origin can write to sessionStorage, and the user can edit it by
			// hand, so an unreadable value is reachable without a bug on our side. Matches
			// ApplicationDataKeyValueStorage: read it back as absent rather than throwing from
			// every caller of the storage.
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				// LogWarning(ex, ...), not LogWarningMessage(msg, ex.Message): the helper's second
				// parameter is [CallerMemberName], so passing ex.Message there dropped the reason
				// and corrupted the caller tag. The sibling ApplicationDataKeyValueStorage still
				// has that bug; don't copy it back.
				Logger.LogWarning(ex, "Error getting value for key '{Key}'.", name);
			}

			return default;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask InternalSetAsync<T>(string name, T value, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		SessionStorageImports.SetItem(GetKey(name), Serializer.ToString(value));

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Value for key '{name}' set.");
		}
	}

	private static string GetKey(string name) => name + KeyNameSuffix;

	private static string GetName(string key) =>
		key.EndsWith(KeyNameSuffix, StringComparison.Ordinal)
			? key.Substring(0, key.Length - KeyNameSuffix.Length)
			: key;
}

/// <summary>
/// <c>globalThis.sessionStorage</c> bindings for <see cref="SessionStorageKeyValueStorage"/>.
/// </summary>
/// <remarks>
/// Top-level rather than nested because the JS interop generator emits a partial declaration of
/// every containing type, which would force the storage record itself to be partial for no other
/// reason. Same shape as <c>Uno.Extensions.Hosting.Imports</c>.
/// </remarks>
internal static partial class SessionStorageImports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.sessionStorage.getItem")]
	public static partial string? GetItem(string key);

	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.sessionStorage.setItem")]
	public static partial void SetItem(string key, string value);

	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.sessionStorage.removeItem")]
	public static partial void RemoveItem(string key);

	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.sessionStorage.key")]
	public static partial string? Key(int index);
}
