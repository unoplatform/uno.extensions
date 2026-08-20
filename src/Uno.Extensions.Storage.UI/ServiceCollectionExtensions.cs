using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Uno.Extensions;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddFileStorage(this IServiceCollection services)
		=> services
			.AddSingleton<IStorage, FileStorage>();

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(InMemoryKeyValueStorage))]
	private static TKeyValueStorage CreateKeyValueStorage<TKeyValueStorage>(
		this IServiceProvider sp,
		string name,
		Func<ILogger<TKeyValueStorage>, InMemoryKeyValueStorage, KeyValueStorageSettings, ISerializer, ISettings, TKeyValueStorage> creator)
	{
		var l = sp.GetRequiredService<ILogger<TKeyValueStorage>>();
		var s = sp.GetRequiredService<ISerializer>();
		var inmem = sp.GetRequiredService<InMemoryKeyValueStorage>();
		var config = sp.GetRequiredService<IOptions<KeyValueStorageConfiguration>>();
		var settings = config.Value.GetSettingsOrDefault(name);
		var unpackaged = sp.GetRequiredService<ISettings>();
		return creator(l, inmem, settings, s, unpackaged);
	}

	/// <summary>
	/// Configuration key selecting the browser store; see <see cref="BrowserCacheLocation"/>.
	/// </summary>
	/// <remarks>
	/// This is the ONLY reader of the value. It is read from raw configuration rather than through
	/// <c>IOptions</c> because it picks a *named* registration, which <c>SetDefaultInstance</c>
	/// resolves here at registration time - before any options are bound. Deliberately not also a
	/// bound property on <see cref="KeyValueStorageConfiguration"/>: two readers of one key meant
	/// two different answers for a bad value, and the "invalid values are rejected" guarantee was
	/// an accident of whether anything happened to resolve the options type.
	/// <para>
	/// The section is the one <c>UseStorage</c> already binds, so the value sits with the rest of
	/// the storage settings and is independent of what an app calls its authentication providers.
	/// </para>
	/// </remarks>
	private const string BrowserCacheLocationKey =
		$"{nameof(KeyValueStorageConfiguration)}:{nameof(BrowserCacheLocation)}";

	/// <summary>
	/// Name of the <see cref="IKeyValueStorage"/> the browser defaults to, per
	/// <see cref="BrowserCacheLocationKey"/>.
	/// </summary>
	/// <remarks>
	/// Absent means <see cref="BrowserCacheLocation.LocalStorage"/> - the store WebAssembly used
	/// before this setting existed, so upgrading a package never relocates an app's data. Anything
	/// present but not a defined member throws: this decides where credentials are kept, and a typo
	/// silently choosing for you is the one outcome worth failing a startup over.
	/// </remarks>
	/// <exception cref="InvalidOperationException">The configured value is not a known location.</exception>
	private static BrowserCacheLocation ResolveBrowserCacheLocation(IConfiguration? configuration)
	{
		var configured = configuration?[BrowserCacheLocationKey];
		if (configured is not { Length: > 0 })
		{
			return BrowserCacheLocation.LocalStorage;
		}

		// IsDefined as well as TryParse: TryParse succeeds for any numeric string, in range *or out*
		// of it, so "3" would otherwise arrive as an undefined enum value and fall to a default -
		// exactly the input a hand-edited config is likeliest to contain. ignoreCase matches what
		// configuration binding would have accepted, so msal-browser's own "sessionStorage"
		// spelling works.
		if (!Enum.TryParse<BrowserCacheLocation>(configured, ignoreCase: true, out var location) ||
			!Enum.IsDefined(location))
		{
			throw new InvalidOperationException(
				$"'{configured}' is not a valid {nameof(BrowserCacheLocation)} for configuration key " +
				$"'{BrowserCacheLocationKey}'. Expected one of: {string.Join(", ", Enum.GetNames<BrowserCacheLocation>())}.");
		}

		return location;
	}

	/// <summary>
	/// Name of the <see cref="IKeyValueStorage"/> registration for a browser cache location.
	/// </summary>
	private static string BrowserStorageName(IConfiguration? configuration) =>
		ResolveBrowserCacheLocation(configuration) switch
		{
			BrowserCacheLocation.SessionStorage => SessionStorageKeyValueStorage.Name,
			BrowserCacheLocation.MemoryStorage => InMemoryKeyValueStorage.Name,
			_ => ApplicationDataKeyValueStorage.Name,
		};

	public static IServiceCollection AddKeyedStorage(this IServiceCollection services, IConfiguration? configuration)
	{
		// Validated on every platform even though only the browser acts on it: a typo in a shared
		// appsettings.json has to fail on the desktop run a developer actually does, not only in the
		// one head that reads the value - which is also the only way the guarantee is testable,
		// since the WebAssembly runtime-test lane cannot run today.
		_ = ResolveBrowserCacheLocation(configuration);

		return services
				.AddNamedSingleton<IKeyValueStorage, InMemoryKeyValueStorage>(InMemoryKeyValueStorage.Name)
				.AddNamedSingleton<IKeyValueStorage, ApplicationDataKeyValueStorage>(
					ApplicationDataKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<ApplicationDataKeyValueStorage>(
								ApplicationDataKeyValueStorage.Name,
								(l, inmem, settings, s, unpackaged) => new ApplicationDataKeyValueStorage(l, inmem, settings, s, unpackaged)
								)
					)
				.AddNamedSingleton<IKeyValueStorage, SessionStorageKeyValueStorage>(
					SessionStorageKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<SessionStorageKeyValueStorage>(
								SessionStorageKeyValueStorage.Name,
								(l, inmem, settings, s, unpackaged) => new SessionStorageKeyValueStorage(l, inmem, settings, s)
								)
					)
#if __ANDROID__
				.AddNamedSingleton<IKeyValueStorage, KeyStoreKeyValueStorage>(
					KeyStoreKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<KeyStoreKeyValueStorage>(
								KeyStoreKeyValueStorage.Name,
								(l, inmem, settings, s, unpackaged) => new KeyStoreKeyValueStorage(l, inmem, settings, s)
								)
					)
#endif
#if __IOS__
				.AddNamedSingleton<IKeyValueStorage, KeyChainKeyValueStorage>(
					KeyChainKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<KeyChainKeyValueStorage>(
								KeyChainKeyValueStorage.Name,
								(l, inmem, settings, s, unpackaged) => new KeyChainKeyValueStorage(l, inmem, settings, s)
								)
					)
#endif
#if !WINUI && (__ANDROID__ || __IOS__ || WINDOWS_UWP)
				.AddSingleton(new PasswordVaultResourceNameProvider((Assembly.GetEntryAssembly()?? Assembly.GetCallingAssembly()?? Assembly.GetExecutingAssembly()).GetName().Name??nameof(PasswordVaultKeyValueStorage)))
				.AddNamedSingleton<IKeyValueStorage, PasswordVaultKeyValueStorage>(PasswordVaultKeyValueStorage.Name)
#endif
#if WINDOWS
				.AddNamedSingleton<IKeyValueStorage, EncryptedApplicationDataKeyValueStorage>(
					EncryptedApplicationDataKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<EncryptedApplicationDataKeyValueStorage>(
								EncryptedApplicationDataKeyValueStorage.Name,
								(l, inmem, settings, s, unpackaged) => new EncryptedApplicationDataKeyValueStorage(l, inmem, settings, s, unpackaged)
								)
					)
#endif
				.SetDefaultInstance<IKeyValueStorage>(
#if __ANDROID__
					KeyStoreKeyValueStorage.Name
#elif __IOS__
					KeyChainKeyValueStorage.Name
#elif WINDOWS
					EncryptedApplicationDataKeyValueStorage.Name
#else
					// Runtime, not compile-time: Skia desktop lands in this branch too, and there
					// ApplicationData is a file rather than browser storage. Only the browser
					// follows the configured cache location.
					OperatingSystem.IsBrowser()
						? BrowserStorageName(configuration)
						// For platforms where we don't currently have a secure storage option, we
						// default to ApplicationDataKeyValueStorage to avoid security concerns
						// with saving plain text
						: ApplicationDataKeyValueStorage.Name
#endif
					);
	}


}
