using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation;

public static class ServiceProviderExtensions
{
	public static void AddInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
	{
		provider.AddInstance(typeof(T), instanceCreator);
	}

	public static void AddInstance<T>(this IServiceProvider provider, Type serviceType, Func<T> instanceCreator)
	{
		provider.GetRequiredService<IInstanceRepository>().Instances[serviceType] = instanceCreator;
	}

	public static T AddInstance<T>(this IServiceProvider provider, T instance)
	{
#pragma warning disable CS8604 // Possible null reference argument.
		provider.AddInstance(typeof(T), instance);
#pragma warning restore CS8604 // Possible null reference argument.
		return instance;
	}

	public static object AddInstance(this IServiceProvider provider, Type serviceType, object instance)
	{
		provider.GetRequiredService<IInstanceRepository>().Instances[serviceType] = instance;
		return instance;
	}

	public static T? GetInstance<T>(this IServiceProvider provider)
	{
		var value = provider.GetInstance(typeof(T));
		if (value is Func<T> valueCreator)
		{
			var instance = valueCreator();
			provider.AddInstance(instance);
			return instance;
		}

		if (value is T typedValue)
		{
			return typedValue;
		}

		return default;
	}

	public static object? GetInstance(this IServiceProvider provider, Type type)
	{
		return provider.GetRequiredService<IInstanceRepository>().Instances.TryGetValue(type, out var value) ? value : null;
	}

	public static IServiceProvider CloneNavigationScopedServices(this IServiceProvider services)
    {
        var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        scopedServices.GetRequiredService<RegionControlProvider>().RegionControl = services.GetRequiredService<RegionControlProvider>().RegionControl;
        var instance = services.GetInstance<INavigator>();
        if (instance is not null)
        {
            scopedServices.AddInstance<INavigator>(instance);
        }

        return scopedServices;
    }

	public static FrameworkElement NavigationHost(this IServiceProvider services)
	{
		var cc = new ContentControl();
		cc.HorizontalAlignment = HorizontalAlignment.Stretch;
		cc.VerticalAlignment = VerticalAlignment.Stretch;
		cc.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		cc.VerticalContentAlignment = VerticalAlignment.Stretch;
		// Create the Root region
		var elementRegion = new NavigationRegion(cc, services);
		cc.SetInstance(elementRegion);

		elementRegion.Navigator()?.NavigateRouteAsync(cc, "") ;

		return cc;
	}
}
