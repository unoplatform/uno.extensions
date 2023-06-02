namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Navigation request handler for tap event
/// </summary>
/// <param name="HandlerLogger">Logger for logging</param>
/// <param name="Resolver">Resolver for navigation</param>
public record TapRequestHandler(ILogger<TapRequestHandler> HandlerLogger, IRouteResolver Resolver) : ActionRequestHandlerBase<FrameworkElement>(HandlerLogger, Resolver)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		if (view is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: view is null");
			}
			return null;
		}

		return BindAction(view,
			action => new TappedEventHandler((sender, args) => action((FrameworkElement)sender, args)),
			(element, handler) => element.Tapped += handler,
			(element, handler) => element.Tapped -= handler);
	}
}
