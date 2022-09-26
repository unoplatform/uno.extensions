namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddViewModelData<TData>(this IServiceCollection services)
		where TData : class
	{
#pragma warning disable CS8603 // Possible null reference return - null data is possible
		return services
					.AddTransient<TData>(services => services.GetRequiredService<NavigationDataProvider>().GetData<TData>());
#pragma warning restore CS8603 // Possible null reference return.
	}

	public static IServiceCollection AddViewModelData(this IServiceCollection services, Type dataType)
	{
#pragma warning disable CS8603 // Possible null reference return - null data is possible
		return services
					.AddTransient(dataType, services => services.GetRequiredService<NavigationDataProvider>().GetData(dataType));
#pragma warning restore CS8603 // Possible null reference return.
	}
}
