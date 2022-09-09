using System.Reflection;

namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{

	public static IServiceCollection AddFileStorage(this IServiceCollection services)
	{
		return services
			.AddSingleton<IStorage, FileStorage>();
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
				.AddSingleton(new PasswordVaultResourceNameProvider((Assembly.GetEntryAssembly()?? Assembly.GetCallingAssembly()?? Assembly.GetExecutingAssembly()).GetName().Name??nameof(PasswordVaultKeyValueStorage)))
				.AddNamedSingleton<IKeyValueStorage, PasswordVaultKeyValueStorage>(PasswordVaultKeyValueStorage.Name)
#endif
#if WINDOWS
				.AddNamedSingleton<IKeyValueStorage, EncryptedApplicationDataKeyValueStorage>(EncryptedApplicationDataKeyValueStorage.Name)
#endif
				.AddSingleton<SecureStorage>(
#if WINUI
#if __ANDROID__
					new SecureStorage(KeyStoreSettingsStorage.Name)
#elif __IOS__
					new SecureStorage(KeyChainSettingsStorage.Name)
#elif WINDOWS
					new SecureStorage(EncryptedApplicationDataKeyValueStorage.Name)
#else
					// For WASM and other platforms where we don't currently have
					// a secure storage option, we default to InMemory to avoid
					// security concerns with saving plain text
					new SecureStorage(InMemoryKeyValueStorage.Name)
#endif

#else
#if __ANDROID__
					new SecureStorage(PasswordVaultKeyValueStorage.Name)
#elif __IOS__
					new SecureStorage(PasswordVaultKeyValueStorage.Name)
#elif WINDOWS_UWP
					new SecureStorage(PasswordVaultKeyValueStorage.Name)
#else
					// For WASM and other platforms where we don't currently have
					// a secure storage option, we default to InMemory to avoid
					// security concerns with saving plain text
					new SecureStorage(InMemoryKeyValueStorage.Name)
#endif
#endif

					);
	}
}
