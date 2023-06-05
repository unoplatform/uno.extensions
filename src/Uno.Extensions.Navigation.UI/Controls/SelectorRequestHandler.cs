﻿namespace Uno.Extensions.Navigation.UI;

public class SelectorRequestHandler : ControlRequestHandlerBase<Selector>
{
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewToBind = view;
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
			if (nav is null || navdata is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Qualifiers.None, navdata);
		};

		SelectionChangedEventHandler selectionAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender as Selector;
			if (sender is null ||
				(sender is ListViewBase lvb && lvb.IsItemClickEnabled))
			{
				return;
			}
			var data = sender.GetData() ??
							actionArgs?.AddedItems?.FirstOrDefault() ??
							sender.SelectedItem; // In some cases, AddedItems is null, even though SelectedItem is not null

			if (data is null)
			{
				return;
			}

			await action(sender, data);
		};

		ItemClickEventHandler clickAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender as ListViewBase;
			if (!(sender?.IsItemClickEnabled ?? false))
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
