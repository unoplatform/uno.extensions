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
		if (view is not NavigationView viewList)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view?.GetType()} is not a NavigationView");
			}
			return default;
		}

		async Task Action(FrameworkElement sender, object data)
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

		async void SelectionAction(NavigationView actionSender, NavigationViewSelectionChangedEventArgs actionArgs)
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

			await Action(sender, data);
		}

		async void ClickAction(NavigationView actionSender, NavigationViewItemInvokedEventArgs actionArgs)
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

			await Action(sender, data);
		}

		void Connect()
		{
			viewList.ItemInvoked += ClickAction;
			viewList.SelectionChanged += SelectionAction;

			if (viewList.SelectedItem is not null)
			{
				_ = Action(viewList, viewList.SelectedItem);
			}
		};

		void Disconnect()
		{
			viewList.ItemInvoked -= ClickAction;
			viewList.SelectionChanged -= SelectionAction;
		};


		if (viewList.IsLoaded)
		{
			Connect();
		}

		void LoadedHandler(object s, RoutedEventArgs e)
		{
			Connect();
		}
		viewList.Loaded += LoadedHandler;
		void UnloadedHandler(object s, RoutedEventArgs e)
		{
			Disconnect();
		}
		viewList.Unloaded += UnloadedHandler;
		return new RequestBinding(viewToBind, LoadedHandler, UnloadedHandler);
	}
}
