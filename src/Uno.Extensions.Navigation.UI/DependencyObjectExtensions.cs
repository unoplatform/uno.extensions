namespace Uno.Extensions.Navigation;

public static class DependencyObjectExtensions
{
	public static IDispatcher? Dispatcher(this FrameworkElement element)
	{
		var sp = element.FindServiceProvider();
		return sp?.GetService<IDispatcher>();
	}

	public static INavigator? Navigator(this FrameworkElement element)
    {
        return element.FindRegion()?.Navigator();
    }

    public static IRegion? FindRegion(this FrameworkElement element)
    {
        return element.ServiceForControl(true, element => element.GetInstance());
    }

	public static IServiceProvider? FindServiceProvider(this FrameworkElement element)
	{
		return element.ServiceForControl(true, element => element.GetServiceProvider());
	}

	public static IRegion? FindParentRegion(this FrameworkElement element, out string? routeName)
    {
        var name = element?.GetName() ?? null;

        var parent = element?.GetParent()?.GetInstance();
        if (parent is null)
        {
            parent = element?.Parent.ServiceForControl(true, element =>
            {
                var instance = element.GetInstance();
                if (instance is null &&
                    name is not { Length: > 0 })
                {
                    var route = (element as FrameworkElement)?.GetName();
                    if (route is not null)
                    {
                        name = route;
                    }
                }
                return instance;
            });
        }

        routeName = name;
        return parent;
    }

    public static TService? ServiceForControl<TService>(this DependencyObject element, bool searchParent, Func<DependencyObject, TService> retrieveFromElement)
    {
        if (element is null)
        {
            return default;
        }

        var service = retrieveFromElement(element);
        if (service is not null)
        {
            return service;
        }

        if (!searchParent)
        {
            return default;
        }

        var parent = VisualTreeHelper.GetParent(element);
        return parent.ServiceForControl(searchParent, retrieveFromElement);
    }
}
