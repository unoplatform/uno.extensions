using Uno.Extensions.Navigation.Regions;


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

	private static DispatcherQueue? GetDispatcher(this FrameworkElement? element) =>
#if WINUI
		element?.DispatcherQueue;
#else
		Windows.ApplicationModel.Core.CoreApplication.MainView.DispatcherQueue;
#endif

	public static async Task EnsureLoaded(this FrameworkElement? element)
	{
		if (element is null)
		{
			return;
		}

		var completion = new TaskCompletionSource<bool>();
		element.GetDispatcher().TryEnqueue(async () =>
		{
			await EnsureElementLoaded(element);
			completion.SetResult(true);
		});
		await completion.Task;


//#if !WINDOWS_UWP && !WINUI
//		var count = VisualTreeHelper.GetChildrenCount(element);
//		for (int i = 0; i < count; i++)
//		{
//			var nextElement = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
//			if(nextElement is ContentPresenter)
//			{
//				continue;
//			}
//			await EnsureLoaded(nextElement);
//		}
//#endif

#if __ANDROID__
		// EnsureLoaded can return from LayoutUpdated causing the remaining task to continue from the measure pass.
		// This is problematic as modifying the visual tree during that moment
		// could potentially leave the visual tree in a broken state.
		// By yielding here, we avoid such situation from happening.
		await Task.Yield();
#endif
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
