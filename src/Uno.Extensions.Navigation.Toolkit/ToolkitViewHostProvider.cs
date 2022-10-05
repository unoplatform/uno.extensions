
namespace Uno.Extensions.Navigation.Toolkit;

internal class ToolkitViewHostProvider : IViewHostProvider
{
	public FrameworkElement CreateViewHost(ContentControl? navigationRoot) => (navigationRoot as ExtendedSplashScreen) ?? new ExtendedSplashScreen
	{
		HorizontalAlignment = HorizontalAlignment.Stretch,
		VerticalAlignment = VerticalAlignment.Stretch,
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch
	};

	public void InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation)
	{
		var loading = new LoadingTask(InitialNavigation, contentControl);

		var lv = contentControl as LoadingView;
		if (lv is not null)
		{
			lv.Source = loading;
		}
	}

	private record LoadingTask(Task NavigationTask, FrameworkElement Context) : Uno.Toolkit.ILoadable
	{
		private bool callbackConnected;
		public bool IsExecuting
		{
			get
			{
				Init();

				return !NavigationTask.IsCompleted;
			}
		}

		private void Init()
		{
			if (!callbackConnected)
			{
				callbackConnected = true;
				var dispatcher = new Dispatcher(Context);
				NavigationTask.ContinueWith(async t =>
				{
					dispatcher?.ExecuteAsync(async () =>
					{
						IsExecutingChanged?.Invoke(this, EventArgs.Empty);
					});
				});
			}
		}


		public event EventHandler? IsExecutingChanged;
	}
}
