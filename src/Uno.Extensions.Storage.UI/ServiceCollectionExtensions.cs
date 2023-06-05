namespace Uno.Extensions;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddFileStorage(this IServiceCollection services)
		=> services
			.AddSingleton<IStorage, FileStorage>();

	private static TKeyValueStorage CreateKeyValueStorage<TKeyValueStorage>(
		this IServiceProvider sp,
		string name,
		Func<ILogger<TKeyValueStorage>, InMemoryKeyValueStorage, KeyValueStorageSettings, ISerializer, TKeyValueStorage> creator)
	{
		var l = sp.GetRequiredService<ILogger<TKeyValueStorage>>();
		var s = sp.GetRequiredService<ISerializer>();
		var inmem = sp.GetRequiredService<InMemoryKeyValueStorage>();
		var config = sp.GetRequiredService<IOptions<KeyValueStorageConfiguration>>();
		var settings = config.Value.GetSettingsOrDefault(name);
		return creator(l, inmem, settings, s);
	}

	public static IServiceCollection AddKeyedStorage(this IServiceCollection services)
	{
		return services
				.AddNamedSingleton<IKeyValueStorage, InMemoryKeyValueStorage>(InMemoryKeyValueStorage.Name)
				.AddNamedSingleton<IKeyValueStorage, ApplicationDataKeyValueStorage>(
					ApplicationDataKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<ApplicationDataKeyValueStorage>(
								ApplicationDataKeyValueStorage.Name,
								(l,inmem, settings,s)=>new ApplicationDataKeyValueStorage(l, inmem, settings, s)
								)
					)
#if __ANDROID__
				.AddNamedSingleton<IKeyValueStorage, KeyStoreKeyValueStorage>(
					KeyStoreKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<KeyStoreKeyValueStorage>(
								KeyStoreKeyValueStorage.Name,
								(l, inmem, settings, s) => new KeyStoreKeyValueStorage(l, inmem, settings, s)
								)
					)
#endif
#if __IOS__
				.AddNamedSingleton<IKeyValueStorage, KeyChainKeyValueStorage>(
					KeyChainKeyValueStorage.Name,
					sp => sp.CreateKeyValueStorage<KeyChainKeyValueStorage>(
								KeyChainKeyValueStorage.Name,
								(l, inmem, settings, s) => new KeyChainKeyValueStorage(l, inmem, settings, s)
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
								(l, inmem, settings, s) => new EncryptedApplicationDataKeyValueStorage(l, inmem, settings, s)
								)
					)
#endif
				.SetDefaultInstance<IKeyValueStorage>(
#if WINUI
#if __ANDROID__
					KeyStoreKeyValueStorage.Name
#elif __IOS__
					KeyChainKeyValueStorage.Name
#elif WINDOWS
					EncryptedApplicationDataKeyValueStorage.Name
#else
					// For WASM and other platforms where we don't currently have
					// a secure storage option, we default to InMemory to avoid
					// security concerns with saving plain text
					InMemoryKeyValueStorage.Name
#endif

#else
#if __ANDROID__
					PasswordVaultKeyValueStorage.Name
#elif __IOS__
					PasswordVaultKeyValueStorage.Name
#elif WINDOWS_UWP
					PasswordVaultKeyValueStorage.Name
#else
					// For WASM and other platforms where we don't currently have
					// a secure storage option, we default to InMemory to avoid
					// security concerns with saving plain text
					InMemoryKeyValueStorage.Name
#endif
#endif
					);
	}


}
