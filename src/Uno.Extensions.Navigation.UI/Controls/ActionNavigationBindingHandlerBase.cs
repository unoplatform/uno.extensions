using System;
using System.Linq;
using Uno.Extensions.Options;
#if !WINUI
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
	public abstract class ActionNavigationBindingHandlerBase<TView> : ControlNavigationBindingHandler<TView>
		where TView : FrameworkElement
	{
		protected void BindAction<TElement, TEventHandler>(
			TElement view,
			Func<Action<FrameworkElement>, TEventHandler> eventHandler,
			Action<TElement, TEventHandler> subscribe,
			Action<TElement, TEventHandler> unsubscribe
			)
			where TElement : FrameworkElement
		{
			Action<FrameworkElement> action = async (element) =>
			{
				var path = element.GetRequest();
				var nav = element.Navigator();

				if (nav is null)
				{
					return;
				}

				var data = element.GetData();
				var resultType = data?.GetType();
				var binding = element.GetBindingExpression(Navigation.DataProperty);
				if (binding is not null &&
					binding.DataItem is not null)
				{
					var dataObject = binding.DataItem;
					var bindingPathSegments = binding.ParentBinding.Path.Path.Split('.').ToArray();
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


				if (data is not null ||
					resultType is not null)
				{

					if (binding is not null && binding.ParentBinding.Mode == Windows.UI.Xaml.Data.BindingMode.TwoWay)
					{
						var response = await nav.NavigateRouteForResultAsync(element, path, Schemes.Current, data, resultType: resultType);
						if (response is not null)
						{
							var result = await response.UntypedResult;
							if (result.MatchSome(out var resultValue))
							{
								element.SetData(resultValue);
								binding.UpdateSource();
							}
						}
					}
					else
					{
						await nav.NavigateRouteAsync(element, path, Schemes.Current, data);

					}
				}
				else
				{
					await nav.NavigateRouteAsync(element, path, Schemes.Current);
				}
			};

			var handler = eventHandler(action);

			bool subscribed = false;
			if (view.IsLoaded)
			{
				subscribed = true;
				subscribe(view, handler);
			}

			view.Loaded += (s, e) =>
			{
				if (!subscribed)
				{
					subscribed = true;
					subscribe(view, handler);
				}
			};
			view.Unloaded += (s, e) =>
			{
				if (subscribed)
				{
					subscribed = false;
					unsubscribe(view, handler);
				}
			};
		}

	}
}
