using System.Diagnostics.CodeAnalysis;

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

	public static IServiceCollection AddKeyedStorage(this IServiceCollection services)
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
					// For WASM and other platforms where we don't currently have
					// a secure storage option, we default to ApplicationDataKeyValueStorage to avoid
					// security concerns with saving plain text
					ApplicationDataKeyValueStorage.Name
#endif
					);
	}


}
