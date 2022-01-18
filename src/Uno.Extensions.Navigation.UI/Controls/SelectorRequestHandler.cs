namespace Uno.Extensions.Navigation.UI;

public class SelectorRequestHandler : ControlRequestHandlerBase<Selector>
{
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var _view = view;
		var viewList = view as Selector;
		if (viewList is null)
		{
			return default;
		}

		Func<FrameworkElement, object, Task> action = async (sender, data) =>
		{
			var navdata = sender.GetData() ?? data;
			var path = sender.GetRequest();
			var nav = sender.Navigator();
			if (nav is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Schemes.Current, navdata);
		};

		SelectionChangedEventHandler selectionAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender as Selector;
			if (sender is null)
			{
				return;
			}
			var data = sender.GetData() ?? sender.SelectedItem;

			await action(sender, data);
		};

		ItemClickEventHandler clickAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender as ListViewBase;
			if (sender is null)
			{
				return;
			}
			var data = sender.GetData() ?? actionArgs.ClickedItem;

			await action(sender, data);
		};

		Action? connect = null;
		Action? disconnect = null;
		if (viewList is ListViewBase lv)
		{
			connect = () =>
			{
				lv.ItemClick += clickAction;
				viewList.SelectionChanged += selectionAction;
			};

			disconnect = () =>
			{
				lv.ItemClick -= clickAction;
				viewList.SelectionChanged -= selectionAction;
			};
		}
		else
		{
			connect = () => viewList.SelectionChanged += selectionAction;
			disconnect = () => viewList.SelectionChanged -= selectionAction;
		}

		if (viewList.IsLoaded)
		{
			connect();
		}

		RoutedEventHandler _loadedHandler =  (s, e) =>
		{
			connect();
		};
		viewList.Loaded += _loadedHandler;
		RoutedEventHandler _unloadedHandler = (s, e) =>
		{
			disconnect();
		};
		viewList.Unloaded += _unloadedHandler;
		return new RequestBinding(_view, _loadedHandler, _unloadedHandler);
	}
}
