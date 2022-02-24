namespace Uno.Extensions.Navigation.Navigators;

public class NavigationViewNavigator : SelectorNavigator<Microsoft.UI.Xaml.Controls.NavigationView>
{
	public NavigationViewNavigator(
	ILogger<NavigationViewNavigator> logger,
	IRegion region,
	IResolver resolver,
	RegionControlProvider controlProvider)
	: base(logger, region, resolver, controlProvider)
	{
	}

	protected override FrameworkElement? SelectedItem
	{
		get => Control?.SelectedItem as FrameworkElement;
		set
		{
			if (Control is not null)
			{
				Control.SelectedItem = value;
			}
		}
	}

	protected override IEnumerable<FrameworkElement>? Items => Control?.MenuItems.OfType<FrameworkElement>();

	protected override Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged)
	{
		var control = Control;
		if(control is null)
		{
			return default;
		}

		TypedEventHandler<Microsoft.UI.Xaml.Controls.NavigationView, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs> handler =
			(nv, args) => selectionChanged(nv, args.SelectedItem as FrameworkElement);

		control.SelectionChanged += handler;
		return () => control.SelectionChanged -= handler;

	}
}
