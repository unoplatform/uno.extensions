using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Uno.Extensions.DependencyInjection;

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
				.SetDefaultInstance<IKeyValueStorage>(
#if WINUI
#if __ANDROID__
					KeyStoreSettingsStorage.Name
#elif __IOS__
					KeyChainSettingsStorage.Name
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
