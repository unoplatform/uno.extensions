using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Base class for request handlers that bind to a specific control type with an callback actions that should be
/// called when subscribing and unsubscribing to a control specific event.
/// </summary>
/// <typeparam name="TView">The type of control to handle requests for</typeparam>
/// <param name="Logger">Logger for Logging</param>
/// <param name="Resolver">Resolver to be used in navigation</param>
public abstract record ActionRequestHandlerBase<TView>(ILogger Logger, IRouteResolver Resolver) : ControlRequestHandlerBase<TView>(Logger)
	where TView : FrameworkElement
{
	/// <summary>
	/// Overridable default qualifier to use when navigating to a route without a qualifier.
	/// </summary>
	protected virtual string DefaultQualifier { get; } = Qualifiers.None;

	/// <summary>
	/// Abstraction for creating a request binding that navigates based on an event
	/// </summary>
	/// <typeparam name="TElement">The type of control to create the binding for</typeparam>
	/// <typeparam name="TEventHandler">The type of event handler to be bound</typeparam>
	/// <param name="view">The view to bind to </param>
	/// <param name="eventHandler">The function that returns an event handler for the action</param>
	/// <param name="subscribe">Callback to subscribe the event handler</param>
	/// <param name="unsubscribe">Callback to unsubscribe the event handler</param>
	/// <returns></returns>
	protected IRequestBinding? BindAction<TElement, TEventHandler>(
		TElement view,
		Func<Action<FrameworkElement, RoutedEventArgs?>, TEventHandler> eventHandler,
		Action<TElement, TEventHandler> subscribe,
		Action<TElement, TEventHandler> unsubscribe
		)
		where TElement : FrameworkElement
	{
		var viewToBind = view;

		async void Action(FrameworkElement element, RoutedEventArgs? eventArgs)
		{
			var path = element.GetRequest();
			var nav = element.Navigator();

			if (nav is null)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarningMessage("No navigator found");
				}
				return;
			}

			var routeHint = new RouteHint { Route = path };

			var data = element.GetData() ?? element.GetDataFromOriginalSource(eventArgs?.OriginalSource);
			var resultType = data?.GetType();

			var binding = element.GetBindingExpression(Navigation.DataProperty);
			if (binding is not null &&
				binding.DataItem is not null)
			{
				var dataObject = binding.DataItem;
				var bindingPathSegments = binding.ParentBinding.Path?.Path?.Split('.').ToArray() ?? Array.Empty<string>();
				for (var i = 0; i < bindingPathSegments.Length; i++)
				{
					var segment = bindingPathSegments[i];
					if (Logger.IsEnabled(LogLevel.Trace))
					{
						Logger.LogTraceMessage($"Attempting to retrieve binding segment: {segment}");
					}
					var prop = GetProperty(dataObject.GetType(), segment);
					if (i == bindingPathSegments.Length - 1)
					{
						resultType = prop?.PropertyType;
						if (Logger.IsEnabled(LogLevel.Trace))
						{
							Logger.LogTraceMessage(resultType is not null ? $"Result type {resultType.Name}" : "No result type");
						}
					}
					else
					{
						dataObject = prop?.GetValue(dataObject);
					}

					if (dataObject is null)
					{
						if (Logger.IsEnabled(LogLevel.Trace))
						{
							Logger.LogTraceMessage("Binding property returned null");
						}
						break;
					}
				}
			}
			routeHint = routeHint with
			{
				Result = resultType
			};

			var qualifier = path.HasQualifier() ? Qualifiers.None : DefaultQualifier;

			path = path ?? string.Empty;

			var response = await nav.NavigateRouteHintAsync(routeHint, element, data, CancellationToken.None);

			if (binding?.ParentBinding.Mode == BindingMode.TwoWay)
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage("Two way binding, so waiting for result of navigation response");
				}
				var resultResponse = response?.AsResultResponse();
				if (resultResponse is not null)
				{
					var result = await resultResponse.UntypedResult;
					if (result.IsSome(out var resultValue))
					{
						if (Logger.IsEnabled(LogLevel.Trace))
						{
							Logger.LogTraceMessage("Data returned from navigation so setting Data attached property and updating binding");
						}

						element.SetData(resultValue);
						binding.UpdateSource();
					}
					else
					{
						if (Logger.IsEnabled(LogLevel.Trace))
						{
							Logger.LogTraceMessage("No data returned from navigation");
						}
					}
				}
			}
		}

		var handler = eventHandler(Action);

		var subscribed = false;
		if (view.IsLoaded)
		{
			subscribed = true;
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage("Subscribing");
			}
			subscribe(viewToBind, handler);
		}

		void LoadedHandler(object s, RoutedEventArgs e)
		{
			if (!subscribed)
			{
				subscribed = true;
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage("Subscribing");
				}
				subscribe(viewToBind, handler);
			}
		}
		view.Loaded += LoadedHandler;
		void UnloadedHandler(object s, RoutedEventArgs e)
		{
			if (subscribed)
			{
				subscribed = false;
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage("Unsubscribing");
				}
				unsubscribe(viewToBind, handler);
			}
		}
		view.Unloaded += UnloadedHandler;

		return new RequestBinding(viewToBind, LoadedHandler, UnloadedHandler);

		[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "presence of property is optional")]
		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "presence of property is optional")]
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "presence of property is optional")]
		static PropertyInfo? GetProperty(Type type, string propertyName)
			=> type.GetProperty(propertyName);
	}
}
