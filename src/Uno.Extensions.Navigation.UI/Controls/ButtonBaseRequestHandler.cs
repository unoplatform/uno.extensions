namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Handler for navigation request for a ButtonBase.
/// </summary>
/// <param name="HandlerLogger">Logger for Logging</param>
/// <param name="Resolver">Resolve for navigation</param>
public sealed record ButtonBaseRequestHandler(ILogger<ButtonBaseRequestHandler> HandlerLogger, IRouteResolver Resolver) : ActionRequestHandlerBase<ButtonBase>(HandlerLogger, Resolver)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewButton = view as ButtonBase;
		if (viewButton is null)
		{
			if(Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view?.GetType()} is not a ButtonBase");
			}
			return null;
		}

		return BindAction(viewButton,
			action => new RoutedEventHandler((sender, args) =>
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage("Button clicked");
				}
				action((ButtonBase)sender, args);
			}),
			(element, handler) => element.Click += handler,
			(element, handler) => element.Click -= handler);
	}
}
