namespace Uno.Extensions.Navigation.Navigators;

public static class FrameNavigatorExtensions
{
	public static readonly DependencyProperty NavigatorInstanceProperty =
		DependencyProperty.RegisterAttached(
			"NavigatorInstance",
			typeof(INavigator),
			typeof(FrameNavigatorExtensions),
			new PropertyMetadata(null));

	public static void SetNavigatorInstance(this FrameworkElement element, INavigator value)
	{
		element.SetValue(NavigatorInstanceProperty, value);
	}

	public static INavigator GetNavigatorInstance(this FrameworkElement element)
	{
		return (INavigator)element.GetValue(NavigatorInstanceProperty);
	}
}
