using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.UI;

public static class Region
{
	public static readonly DependencyProperty InstanceProperty =
	   DependencyProperty.RegisterAttached(
		   "Instance",
		   typeof(IRegion),
		   typeof(Region),
		   new PropertyMetadata(null));

	public static readonly DependencyProperty AttachedProperty =
		DependencyProperty.RegisterAttached(
			"Attached",
			typeof(bool),
			typeof(Region),
			new PropertyMetadata(false, AttachedChanged));

	public static readonly DependencyProperty ParentProperty =
		DependencyProperty.RegisterAttached(
			"Parent",
			typeof(FrameworkElement),
			typeof(Region),
			new PropertyMetadata(null));

	public static readonly DependencyProperty NameProperty =
		DependencyProperty.RegisterAttached(
			"Name",
			typeof(string),
			typeof(Region),
			new PropertyMetadata(null));

	public static readonly DependencyProperty NavigatorProperty =
		DependencyProperty.RegisterAttached(
			"Navigator",
			typeof(string),
			typeof(Region),
			new PropertyMetadata(null));

	public static readonly DependencyProperty ServiceProviderProperty =
		DependencyProperty.RegisterAttached(
			"ServiceProvider",
			typeof(IServiceProvider),
			typeof(Region),
			new PropertyMetadata(null));

	private static void AttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
		{
			return;
		}

		if (d is FrameworkElement element)
		{
			RegisterElement(element, e.NewValue is bool active ? active : false);
		}
	}

	private static void RegisterElement(FrameworkElement element, bool active)
	{
		var existingRegion = Region.GetInstance(element);
		if (existingRegion is not null)
		{
			if (active)
			{
				existingRegion.ReassignParent();
			}
			else
			{
				existingRegion.Detach();
			}
		}

		var region = existingRegion ?? (active ? new NavigationRegion(element) : default);
	}

	public static void SetInstance(this DependencyObject element, IRegion? value)
	{
		element.SetValue(InstanceProperty, value);
	}

	public static IRegion GetInstance(this DependencyObject element)
	{
		return (IRegion)element.GetValue(InstanceProperty);
	}

	public static void SetAttached(this DependencyObject element, bool value)
	{
		element.SetValue(AttachedProperty, value);
	}

	public static bool GetAttached(this DependencyObject element)
	{
		return (bool)element.GetValue(AttachedProperty);
	}

	public static void SetParent(DependencyObject element, FrameworkElement value)
	{
		element.SetValue(ParentProperty, value);
	}

	public static FrameworkElement GetParent(this DependencyObject element)
	{
		return (FrameworkElement)element.GetValue(ParentProperty);
	}

	public static void SetName(this FrameworkElement element, string? value)
	{
		element.SetValue(NameProperty, value);
	}

	public static void ReassignRegionParent(this FrameworkElement element)
	{

		var childrenCount = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childrenCount; i++)
		{
			var child = VisualTreeHelper.GetChild(element, i);
			if (child.GetInstance() is IRegion region && (
				string.IsNullOrWhiteSpace(region.Name) ||
				region.Parent is null ||
				region.Parent.Children.IndexOf(region) < 0))
			{
				region.ReassignParent();
			}
			else
			{
				if (child is FrameworkElement childElement)
				{
					ReassignRegionParent(childElement);
				}
			}
		}
	}

	public static string GetName(this FrameworkElement element)
	{
		return (string)element.GetValue(NameProperty);
	}

	public static string? GetRegionOrElementName(this FrameworkElement? element)
	{
		return element is not null ? element.GetName() ?? element.Name : null;
	}

	public static void SetNavigator(this FrameworkElement element, string value)
	{
		element.SetValue(NavigatorProperty, value);
	}

	public static string GetNavigator(this FrameworkElement element)
	{
		return (string)element.GetValue(NavigatorProperty);
	}

	public static void SetServiceProvider(this DependencyObject element, IServiceProvider value)
	{
		element.SetValue(ServiceProviderProperty, value);
	}

	public static IServiceProvider GetServiceProvider(this DependencyObject element)
	{
		return (IServiceProvider)element.GetValue(ServiceProviderProperty);
	}
}
