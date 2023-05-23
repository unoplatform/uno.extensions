using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Navigation request handler for <see cref="NavigationView"/>.
/// </summary>
/// <param name="HandlerLogger">Logger for logging</param>
public sealed record NavigationViewRequestHandler(ILogger<NavigationViewRequestHandler> HandlerLogger) : ControlRequestHandlerBase<NavigationView>(HandlerLogger)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewToBind = view;
		var viewList = view as NavigationView;
		if (viewList is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view?.GetType()} is not a NavigationView");
			}
			return default;
		}

		async Task action(FrameworkElement sender, object data)
		{
			var navdata = sender.GetData() ?? data;
			var path = sender.GetRequest();
			var nav = sender.Navigator();
			if (nav is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Qualifiers.None, navdata);
		}

		async void selectionAction(NavigationView actionSender, NavigationViewSelectionChangedEventArgs actionArgs)
		{
			var sender = actionSender;
			if (sender is null)
			{
				return;
			}

			if (actionArgs.SelectedItemContainer is NavigationViewItem navItem && !navItem.SelectsOnInvoked)
			{
				return;
			}

			var data = sender.GetData() ?? sender.SelectedItem;
			if (data is null)
			{
				return;
			}

			await action(sender, data);
		}

		async void clickAction(NavigationView actionSender, NavigationViewItemInvokedEventArgs actionArgs)
		{
			var sender = actionSender;
			if (sender is null)
			{
				return;
			}

			if (actionArgs.InvokedItemContainer is NavigationViewItem navItem && navItem.SelectsOnInvoked)
			{
				return;
			}

			var data = sender.GetData() ?? actionArgs.InvokedItem;
			if (data is null)
			{
				return;
			}

			await action(sender, data);
		}

		Action? connect = null;
		Action? disconnect = null;

		connect = () =>
		{
			viewList.ItemInvoked += clickAction;
			viewList.SelectionChanged += selectionAction;

			if (viewList.SelectedItem is not null)
			{
				_ = action(viewList, viewList.SelectedItem);
			}
		};

		disconnect = () =>
		{
			viewList.ItemInvoked -= clickAction;
			viewList.SelectionChanged -= selectionAction;
		};


		if (viewList.IsLoaded)
		{
			connect();
		}

		void loadedHandler(object s, RoutedEventArgs e)
		{
			connect();
		}
		viewList.Loaded += loadedHandler;
		void unloadedHandler(object s, RoutedEventArgs e)
		{
			disconnect();
		}
		viewList.Unloaded += unloadedHandler;
		return new RequestBinding(viewToBind, loadedHandler, unloadedHandler);
	}
}
