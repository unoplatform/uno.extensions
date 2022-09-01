



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
				.AddNamedSingleton<IKeyValueStorage, InMemoryKeyValueStorage>(InMemoryKeyValueStorage.Name);
				//.AddNamedSingleton<IKeyedStorage, ApplicationDataKeyedStorage>(ApplicationDataKeyedStorage.Name);
	}
}
