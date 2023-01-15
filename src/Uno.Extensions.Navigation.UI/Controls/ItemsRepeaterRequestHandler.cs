#if !WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.UI;

public class ItemsRepeaterRequestHandler : ControlRequestHandlerBase<ItemsRepeater>
{
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewToBind = view;
		var viewList = view as ItemsRepeater;
		if (viewList is null)
		{
			return default;
		}

		Func<FrameworkElement, object?, Task> action = async (sender, data) =>
		{
			var navdata = data;
			var path = sender.GetRequest();
			var nav = sender.Navigator();
			if (nav is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Qualifiers.None, navdata);
		};

		TappedEventHandler tappedAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender as ItemsRepeater;
			if (sender is null)
			{
				return;
			}

			var elt = actionArgs.OriginalSource as DependencyObject;
			while (elt is not null)
			{
				var parent = VisualTreeHelper.GetParent(elt);
				if (parent == sender)
				{
					var itemClicked = (elt as ContentControl)?.Content ?? (elt as FrameworkElement)?.DataContext;
					await action(sender, itemClicked);

				}
				elt = parent;
			}

		};


		Action connect = () => viewList.Tapped += tappedAction;
		Action disconnect = () => viewList.Tapped -= tappedAction;

		if (viewList.IsLoaded)
		{
			connect();
		}

		RoutedEventHandler loadedHandler = (s, e) =>
		{
			connect();
		};
		viewList.Loaded += loadedHandler;
		RoutedEventHandler unloadedHandler = (s, e) =>
		{
			disconnect();
		};
		viewList.Unloaded += unloadedHandler;
		return new RequestBinding(viewToBind, loadedHandler, unloadedHandler);
	}
}
