namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Base class for request handlers that bind to a specific control type.
/// </summary>
/// <typeparam name="TControl">The type of control to handle requests for</typeparam>
/// <param name="Logger">Logger for logging</param>
public abstract record ControlRequestHandlerBase<TControl>(ILogger Logger) : IRequestHandler
{
	/// <inheritdoc/>
	public abstract IRequestBinding? Bind(FrameworkElement view);

	/// <inheritdoc/>
	public bool CanBind(FrameworkElement view)
	{
		var controlType = typeof(TControl);

		var viewType = view.GetType();
		if (viewType == controlType)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"CanBind: {viewType} is {controlType}");
			}

			return true;
		}

		var baseTypes = viewType.GetBaseTypes();
		var baseMatch = baseTypes.FirstOrDefault(baseType => baseType == controlType);
		if (baseMatch is not null &&
			Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"CanBind: {viewType} inherits from {baseMatch} that is {controlType}");
		}
		return baseMatch is not null;
	}
}
