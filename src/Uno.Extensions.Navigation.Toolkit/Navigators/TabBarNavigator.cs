namespace Uno.Extensions.Navigation.Toolkit.Navigators;

public class TabBarNavigator : SelectorNavigator<TabBar>
{
	public TabBarNavigator(
	ILogger<TabBarNavigator> logger,
	IRegion region,
	IRouteResolver routeResolver,
	RegionControlProvider controlProvider)
	: base(logger, region, routeResolver, controlProvider)
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

	protected override IEnumerable<FrameworkElement>? Items => Control?.Items.OfType<FrameworkElement>();

	protected override Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged)
	{
		var control = Control;
		if (control is null)
		{
			return default;
		}

		TypedEventHandler<TabBar, TabBarSelectionChangedEventArgs> handler =
			(nv, args) => selectionChanged(nv, args.NewItem as FrameworkElement);

		control.SelectionChanged += handler;
		return () => control.SelectionChanged -= handler;

	}
}
