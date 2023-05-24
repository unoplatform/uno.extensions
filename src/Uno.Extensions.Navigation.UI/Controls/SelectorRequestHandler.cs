namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Navigation request handler for <see cref="Selector"/> controls.
/// </summary>
/// <param name="HandlerLogger">Logger for logging</param>
public record SelectorRequestHandler(ILogger<SelectorRequestHandler> HandlerLogger) : ControlRequestHandlerBase<Selector>(HandlerLogger)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewToBind = view;
		var viewList = view as Selector;
		if (viewList is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view?.GetType()} is not a Selector");
			}
			return default;
		}

		async Task Action(FrameworkElement sender, object data)
		{
			var navdata = sender.GetData() ?? data;
			var path = sender.GetRequest();
			var nav = sender.Navigator();
			if (nav is null || navdata is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Qualifiers.None, navdata);
		}

		async void SelectionAction(object actionSender, SelectionChangedEventArgs actionArgs)
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

			await Action(sender, data);
		}

		async void ClickAction(object actionSender, ItemClickEventArgs actionArgs)
		{
			var sender = actionSender as ListViewBase;
			if (!(sender?.IsItemClickEnabled ?? false))
			{
				return;
			}
			var data = sender.GetData() ?? actionArgs.ClickedItem;

			await Action(sender, data);
		}

		Action? connect = null;
		Action? disconnect = null;
		if (viewList is ListViewBase lv)
		{
			connect = () =>
			{
				lv.ItemClick += ClickAction;
				viewList.SelectionChanged += SelectionAction;
			};

			disconnect = () =>
			{
				lv.ItemClick -= ClickAction;
				viewList.SelectionChanged -= SelectionAction;
			};
		}
		else
		{
			connect = () => viewList.SelectionChanged += SelectionAction;
			disconnect = () => viewList.SelectionChanged -= SelectionAction;
		}

		if (viewList.IsLoaded)
		{
			connect();
		}

		void LoadedHandler(object s, RoutedEventArgs e)
		{
			connect();
		}
		viewList.Loaded += LoadedHandler;
		void UnloadedHandler(object s, RoutedEventArgs e)
		{
			disconnect();
		}
		viewList.Unloaded += UnloadedHandler;
		return new RequestBinding(viewToBind, LoadedHandler, UnloadedHandler);
	}
}
