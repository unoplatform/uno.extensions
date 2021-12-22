namespace Uno.Extensions.Navigation.UI;

public static class Navigation
{
    public static readonly DependencyProperty RequestProperty =
        DependencyProperty.RegisterAttached(
            "Request",
            typeof(string),
            typeof(Navigation),
            new PropertyMetadata(null, RequestChanged));

    public static readonly DependencyProperty DataProperty =
    DependencyProperty.RegisterAttached(
        "Data",
        typeof(object),
        typeof(Navigation),
        new PropertyMetadata(null));

    private static void RequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
        {
            return;
        }

        if (d is FrameworkElement element)
        {
            _ = new NavigationRequestBinder(element);
        }
    }

    public static void SetRequest(this FrameworkElement element, string value)
    {
        element.SetValue(RequestProperty, value);
    }

    public static string GetRequest(this FrameworkElement element)
    {
        return (string)element.GetValue(RequestProperty);
    }

    public static void SetData(this FrameworkElement element, object? value)
    {
        element.SetValue(DataProperty, value);
    }

    public static object GetData(this FrameworkElement element)
    {
        return (object)element.GetValue(DataProperty);
    }

    //public static string NavigationRoute(this object view, IRouteMappings mappings = null)
    //{
    //    var map = mappings?.FindByView(view.GetType());
    //    if (map is not null)
    //    {
    //        return map.Path;
    //    }

    //    if (view is FrameworkElement fe)
    //    {
    //        var path = fe.GetName();
    //        if (string.IsNullOrWhiteSpace(path))
    //        {
    //            path = fe.Name;
    //        }

    //        return path;
    //    }

    //    return null;
    //}
}
