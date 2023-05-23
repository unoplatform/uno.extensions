#if !WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Navigation handler for the ItemsRepeater control.
/// </summary>
/// <param name="HandlerLogger">Logger for Logging</param>
public sealed record ItemsRepeaterRequestHandler(ILogger<ItemsRepeaterRequestHandler> HandlerLogger) : ControlRequestHandlerBase<ItemsRepeater>(HandlerLogger)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewToBind = view;
		var viewList = view as ItemsRepeater;
		if (viewList is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view?.GetType()} is not an ItemsRepeater");
			}

			return default;
		}

		async Task action(FrameworkElement sender, object? data)
		{
			var navdata = data;
			var path = sender.GetRequest();
			var nav = sender.Navigator();
			if (nav is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Qualifiers.None, navdata);
		}

		var isCaptured = false;
		object? dataContext = default;
		FrameworkElement? pointerElement = default;
		void pointerPressed(object actionSender, PointerRoutedEventArgs actionArgs)
		{
			var sender = actionSender as ItemsRepeater;
			if (sender is null)
			{
				return;
			}

			isCaptured = sender.CapturePointer(actionArgs.Pointer);
			if (isCaptured)
			{
				actionArgs.Handled = true;
				dataContext = default; // Reset from any prior pointer pressed events
				var elt = actionArgs.OriginalSource as DependencyObject;
				while (elt is not null)
				{
					var parent = VisualTreeHelper.GetParent(elt);
					if (parent == sender)
					{
						pointerElement = elt as FrameworkElement;
						dataContext = (elt as FrameworkElement)?.DataContext;
					}
					elt = parent;
				}
			}

		}

		async void pointerReleased(object actionSender, PointerRoutedEventArgs actionArgs)
		{
			var sender = actionSender as ItemsRepeater;
			if (sender is null)
			{
				return;
			}

			if (isCaptured)
			{
				pointerElement ??= sender;
				var pointerPosition = actionArgs.GetCurrentPoint(pointerElement).Position;

				// Tolerance is consistent with use in ButtonBase
				// https://github.com/unoplatform/uno/blob/4bc829e4ef01f2b89733b74c61a3161780818a8b/src/Uno.UI/UI/Xaml/Controls/Primitives/ButtonBase/ButtonBase.mux.cs#LL490C4-L494C88
				const double tolerance = 0.05;
				var layoutRect = LayoutInformation.GetLayoutSlot(pointerElement);
				var pointerInControl =
					-tolerance <= pointerPosition.X && pointerPosition.X <= layoutRect.Width + tolerance &&
					-tolerance <= pointerPosition.Y && pointerPosition.Y <= layoutRect.Height + tolerance;
				if (pointerInControl)
				{
					actionArgs.Handled = true;
					await action(sender, dataContext);
				}

				isCaptured = false;
				dataContext = null;
				pointerElement = null;
			}

		}


		void connect()
		{
			viewList.PointerPressed += pointerPressed;
			viewList.PointerReleased += pointerReleased;
		}
		void disconnect()
		{
			viewList.PointerPressed -= pointerPressed;
			viewList.PointerReleased -= pointerReleased;
		}

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
