﻿namespace Uno.Extensions.Navigation.UI;

public abstract class ActionRequestHandlerBase<TView> : ControlRequestHandlerBase<TView>
	where TView : FrameworkElement
{
	private readonly IRouteResolver _resolver;
	protected ActionRequestHandlerBase(IRouteResolver routes)
	{
		_resolver = routes;
	}

	protected string DefaultQualifier { get; init; } = Qualifiers.None;

	protected IRequestBinding? BindAction<TElement, TEventHandler>(
		TElement view,
		Func<Action<FrameworkElement, RoutedEventArgs?>, TEventHandler> eventHandler,
		Action<TElement, TEventHandler> subscribe,
		Action<TElement, TEventHandler> unsubscribe
		)
		where TElement : FrameworkElement
	{
		var viewToBind = view;
		Action<FrameworkElement, RoutedEventArgs?> action = async (element, eventArgs) =>
		{
			var path = element.GetRequest();
			var nav = element.Navigator();

			if (nav is null)
			{
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
				for (int i = 0; i < bindingPathSegments.Length; i++)
				{
					var prop = dataObject.GetType().GetProperty(bindingPathSegments[i]);
					if (i == bindingPathSegments.Length - 1)
					{
						resultType = prop?.PropertyType;
					}
					else
					{
						dataObject = prop?.GetValue(dataObject);
					}

					if (dataObject is null)
					{
						break;
					}
				}
			}
			routeHint = routeHint with
			{
				//Data = data?.GetType(),
				Result = resultType
			};

			var qualifier = path.HasQualifier() ? Qualifiers.None : DefaultQualifier;

			path = path ?? string.Empty;

			var response = await nav.NavigateRouteHintAsync(routeHint, element, data, CancellationToken.None);

			if (binding is not null &&
			binding.ParentBinding.Mode == BindingMode.TwoWay)
			{
				var resultResponse = response?.AsResultResponse();
				if (resultResponse is not null)
				{
					var result = await resultResponse.UntypedResult;
					if (result.IsSome(out var resultValue))
					{
						element.SetData(resultValue);
						binding.UpdateSource();
					}
				}
			}
		};

		var handler = eventHandler(action);

		bool subscribed = false;
		if (view.IsLoaded)
		{
			subscribed = true;
			subscribe(viewToBind, handler);
		}

		RoutedEventHandler loadedHandler = (s, e) =>
		{
			if (!subscribed)
			{
				subscribed = true;
				subscribe(viewToBind, handler);
			}
		};
		view.Loaded += loadedHandler;
		RoutedEventHandler unloadedHandler = (s, e) =>
		{
			if (subscribed)
			{
				subscribed = false;
				unsubscribe(viewToBind, handler);
			}
		};
		view.Unloaded += unloadedHandler;

		return new RequestBinding(viewToBind, loadedHandler, unloadedHandler);
	}
}
