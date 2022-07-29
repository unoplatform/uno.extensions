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

}
