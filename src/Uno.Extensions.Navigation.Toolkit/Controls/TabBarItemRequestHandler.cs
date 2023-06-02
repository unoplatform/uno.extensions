namespace Uno.Extensions.Navigation.Toolkit.Controls;

/// <summary>
/// Navigation request handler for <see cref="TabBarItem"/> controls.
/// </summary>
/// <param name="HandlerLogger">Logger for logging</param>
/// <param name="Resolver">Resolver for navigation</param>
public sealed record TabBarItemRequestHandler(ILogger<TabBarItemRequestHandler> HandlerLogger, IRouteResolver Resolver) : ActionRequestHandlerBase<TabBarItem>(HandlerLogger, Resolver)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewButton = view as TabBarItem;
		if (viewButton is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view.GetType()} is not an instance, or is not a TabBarItem");
			}
			return default;
		}

		return BindAction(viewButton,
			action => new RoutedEventHandler((sender, args) => action((TabBarItem)sender, args)),
			(element, handler) => element.Click += handler,
			(element, handler) => element.Click -= handler);
	}
}
