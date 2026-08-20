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
	/// Configuration key holding <see cref="KeyValueStorageConfiguration.BrowserCacheLocation"/>.
	/// </summary>
	/// <remarks>
	/// Read from raw configuration rather than <c>IOptions</c> because the value picks a *named*
	/// registration, which <c>SetDefaultInstance</c> resolves at registration time - before any
	/// options are bound. The section is the one <c>UseStorage</c> already binds, so the value sits
	/// with the rest of the storage settings and is independent of what an app calls its
	/// authentication providers.
	/// </remarks>
	private const string BrowserCacheLocationKey =
		$"{nameof(KeyValueStorageConfiguration)}:{nameof(KeyValueStorageConfiguration.BrowserCacheLocation)}";

	/// <summary>
	/// Name of the <see cref="IKeyValueStorage"/> the browser should default to, following
	/// <see cref="BrowserCacheLocationKey"/>. Anything unparseable - including nothing configured -
	/// takes the default, which is msal-browser's <c>sessionStorage</c>.
	/// </summary>
	private static string BrowserStorageName(IConfiguration? configuration)
	{
		// ignoreCase to match what configuration binding would have done, so an app writing
		// msal-browser's own "sessionStorage" spelling gets the same result.
		_ = Enum.TryParse<BrowserCacheLocation>(configuration?[BrowserCacheLocationKey], ignoreCase: true, out var location);

		return location switch
		{
			BrowserCacheLocation.LocalStorage => ApplicationDataKeyValueStorage.Name,
			BrowserCacheLocation.MemoryStorage => InMemoryKeyValueStorage.Name,
			_ => SessionStorageKeyValueStorage.Name,
		};
	}

	public static IServiceCollection AddKeyedStorage(this IServiceCollection services, IConfiguration? configuration)
	{
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
