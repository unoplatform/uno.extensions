namespace Uno.Extensions.Navigation;

public static class ServiceProviderExtensions
{
	public static void AttachServices(this Window window, IServiceProvider services)
	{
		window.Content
				.AttachServiceProvider(services)
				.RegisterWindow(window);
	}

	internal static IServiceProvider RegisterWindow(this IServiceProvider services, Window window)
	{
		return services.AddInstance(window);
	}

	internal static IServiceProvider CreateNavigationScope(this IServiceProvider services)
	{
		var scoped = services.CreateScope().ServiceProvider;
		return scoped.AddInstance(services.GetRequiredService<Window>());
	}
	

	public static FrameworkElement AttachNavigation(this Window window, IServiceProvider services, string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null)
	{
		var root = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch
		};
		window.Content = root;

		window.AttachServices(services);

		root.Host(initialRoute, initialView, initialViewModel);

		return root;
	}
}
