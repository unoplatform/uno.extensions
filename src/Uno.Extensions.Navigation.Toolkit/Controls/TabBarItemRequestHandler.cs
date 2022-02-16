namespace Uno.Extensions.Navigation.Toolkit.Controls;

public class TabBarItemRequestHandler : ActionRequestHandlerBase<TabBarItem>
{
	public TabBarItemRequestHandler(IRouteResolver routes) : base(routes)
	{
		DefaultScheme = Schemes.ChangeContent;
	}

	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewButton = view as TabBarItem;
		if (viewButton is null)
		{
			return default;
		}

		return BindAction(viewButton,
			action => new RoutedEventHandler((sender, args) => action((TabBarItem)sender)),
			(element, handler) => element.Click += handler,
			(element, handler) => element.Click -= handler);
	}
}
