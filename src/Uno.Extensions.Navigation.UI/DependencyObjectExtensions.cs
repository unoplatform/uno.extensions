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

        // Stop at a navigation boundary: an element with Region.Attached explicitly set to false
        // detaches its subtree from the navigation above it, so descendants resolve no region/SP.
        if (element.IsNavigationBoundary())
        {
            if (Region.Logger.IsEnabled(LogLevel.Trace))
            {
                Region.Logger.LogTraceMessage($"Navigation boundary reached at {element.GetType().Name} (Region.Attached=false); stopping service-provider walk - the subtree below is isolated from the navigation above it.");
            }

            return default;
        }

        var parent = VisualTreeHelper.GetParent(element);
        return parent.ServiceForControl(searchParent, retrieveFromElement);
    }

    /// <summary>
    /// True when Region.Attached marks this element as a navigation boundary: its subtree is not
    /// attached to the navigation above it. Requires a <em>local</em> <c>false</c> (so the default
    /// unset-false, which every element has, is not a boundary) <em>and</em> an effective value of
    /// <c>false</c>. The latter excludes the responsive master-detail idiom where a pane is declared
    /// <c>Region.Attached="false"</c> locally but flipped to <c>true</c> by a VisualState/Style setter
    /// for the wide layout - that pane is a real region, not a boundary.
    /// </summary>
    internal static bool IsNavigationBoundary(this DependencyObject element)
        => element.ReadLocalValue(Region.AttachedProperty) is bool local && !local
            && !(bool)element.GetValue(Region.AttachedProperty);
}
