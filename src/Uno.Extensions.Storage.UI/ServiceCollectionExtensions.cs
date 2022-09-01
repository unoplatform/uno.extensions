namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	
	public static IServiceCollection AddFileStorage(this IServiceCollection services)
	{
		return services
			.AddSingleton<IStorage,FileStorage>();
	}

		public static IServiceCollection AddKeyedStorage(this IServiceCollection services)
	{
		return services
				.AddNamedSingleton<IKeyValueStorage, InMemoryKeyValueStorage>(InMemoryKeyValueStorage.Name)
				.AddNamedSingleton<IKeyValueStorage, ApplicationDataKeyValueStorage>(ApplicationDataKeyValueStorage.Name)
#if __ANDROID__
				.AddNamedSingleton<IKeyValueStorage, KeyStoreSettingsStorage>(KeyStoreSettingsStorage.Name)
#endif
#if __IOS__
				.AddNamedSingleton<IKeyValueStorage, KeyChainSettingsStorage>(KeyChainSettingsStorage.Name)
#endif
#if !WINUI && (__ANDROID__ || __IOS__ || WINDOWS_UWP)
				.AddNamedSingleton<IKeyValueStorage, PasswordVaultKeyValueStorage>(PasswordVaultKeyValueStorage.Name)
#endif
#if WINDOWS_UWP
				.AddNamedSingleton<IKeyValueStorage, EncryptedApplicationDataKeyValueStorage>(EncryptedApplicationDataKeyValueStorage.Name)
#endif
				.AddSingleton(
#if WINUI
#if __ANDROID__
					new KeyValueStorageIndex(
						KeyStoreSettingsStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false),
						(KeyStoreSettingsStorage.Name, true))
#elif __IOS__
					new KeyValueStorageIndex(
						KeyChainSettingsStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false),
						(KeyChainSettingsStorage.Name, true))
#elif WINDOWS
					new KeyValueStorageIndex(
						ApplicationDataKeyValueStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false))
else
					new KeyValueStorageIndex(
						ApplicationDataKeyValueStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false))
#endif

#else
#if __ANDROID__
					new KeyValueStorageIndex(
						PasswordVaultKeyValueStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false),
						(KeyStoreSettingsStorage.Name, true),
						(PasswordVaultKeyValueStorage.Name, true))
#elif __IOS__
					new KeyValueStorageIndex(
						PasswordVaultKeyValueStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false),
						(KeyChainSettingsStorage.Name, true),
						(PasswordVaultKeyValueStorage.Name, true))
#elif WINDOW
					new KeyValueStorageIndex(
						PasswordVaultKeyValueStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false),
						(EncryptedApplicationDataKeyValueStorage.Name, true),
						(PasswordVaultKeyValueStorage.Name, true))
else
					new KeyValueStorageIndex(
						ApplicationDataKeyValueStorage.Name,
						(InMemoryKeyValueStorage.Name, false),
						(ApplicationDataKeyValueStorage.Name, false))
#endif
#endif

					)
				;
	}
}
