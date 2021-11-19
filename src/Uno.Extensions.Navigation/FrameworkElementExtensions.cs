using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
#if !WINUI
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
#endif

namespace Uno.Extensions.Navigation;

public static class FrameworkElementExtensions
{
	public static async Task EnsureLoaded(this IRegion region)
	{
		if (region.Services is not null)
		{
			return;
		}

		if (region?.View is null)
		{
			return;
		}

		await region.View.EnsureLoaded();

		if (region.Parent is null)
		{
			return;
		}

		await region.Parent.EnsureLoaded();
	}
	public static async Task EnsureLoaded(this FrameworkElement? element)
	{
		await EnsureElementLoaded(element);

		if(element is null)
		{
			return;
		}

		var count = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < count; i++)
		{
			var nextElement = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
			if(nextElement is ContentPresenter)
			{
				continue;
			}
			await EnsureLoaded(nextElement);
		}

	}
	private static async Task EnsureElementLoaded(this FrameworkElement? element)
	{
		if (element == null)
		{
			return;
		}

		var completion = new TaskCompletionSource<object>();

		// Note: We're attaching to three different events to
		// a) always detect when element is loaded (sometimes Loaded is never fired)
		// b) detect as soon as IsLoaded is true (Loading and Loaded not always in right order)

		RoutedEventHandler? loaded = null;
		EventHandler<object>? layoutChanged = null;
		TypedEventHandler<FrameworkElement, object>? loading = null;

		Action<bool> loadedAction = (overrideLoaded) =>
		{
			if (element.IsLoaded ||
				(element.ActualHeight > 0 && element.ActualWidth > 0))
			{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
				completion.SetResult(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
				element.Loaded -= loaded;
				element.Loading -= loading;
				element.LayoutUpdated -= layoutChanged;
			}
		};

		loaded = (s, e) => loadedAction(false);
		loading = (s, e) => loadedAction(false);
		layoutChanged = (s, e) => loadedAction(true);

		element.Loaded += loaded;
		element.Loading += loading;
		element.LayoutUpdated += layoutChanged;

		if (element.IsLoaded ||
			(element.ActualHeight > 0 && element.ActualWidth > 0))
		{
			loadedAction(false);
		}

		await completion.Task;
	}

	public static void InjectServicesAndSetDataContext(
		this FrameworkElement view,
		IServiceProvider services,
		INavigator navigation,
		object? viewModel)
	{
		if (view is not null)
		{
			if (viewModel is not null &&
				view.DataContext != viewModel)
			{
				view.DataContext = viewModel;
			}
		}

		if (view is IInjectable<INavigator> navAware)
		{
			navAware.Inject(navigation);
		}

		if (view is IInjectable<IServiceProvider> spAware)
		{
			spAware.Inject(services);
		}
	}
}
