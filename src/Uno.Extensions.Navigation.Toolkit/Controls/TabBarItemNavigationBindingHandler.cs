using Uno.Extensions.Navigation.Controls;
using Uno.Toolkit.UI.Controls;

namespace Uno.Extensions.Navigation.Toolkit.Controls;

public class TabBarItemNavigationBindingHandler : ActionNavigationBindingHandlerBase<TabBarItem>
{
	public override void Bind(FrameworkElement view)
	{
		var viewButton = view as TabBarItem;
		if (viewButton is null)
		{
			return;
		}

		BindAction(viewButton,
			action => new RoutedEventHandler((sender, args) => action((TabBarItem)sender)),
			(element, handler) => element.Click += handler,
			(element, handler) => element.Click -= handler);
	}
}
