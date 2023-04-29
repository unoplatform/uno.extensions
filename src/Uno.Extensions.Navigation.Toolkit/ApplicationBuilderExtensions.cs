namespace Uno.Extensions;

public static class ApplicationBuilderExtensions
{
	private struct ToolkitViewInitializer : IRootViewInitializer
	{
		public ContentControl CreateDefaultView() => new LoadingView();

		public void InitializeViewHost(Window window, FrameworkElement element, Task loadingTask)
		{
			//if (element is LoadingView loadingView)
			//{
			//	loadingView.Source = new LoadingTask(loadingTask, element);
			//}

			var activate = true;
			if (element is LoadingView lv)
			{
				var activateTask = loadingTask;
				if (lv is ExtendedSplashScreen splash)
				{
					if (!splash.SplashIsEnabled)
					{
						// Splash isn't enabled, so don't activate until loading completed
						activate = false;

						splash.UseTransitions = false;

						activateTask = new Func<Task>(async () =>
						{
							await loadingTask;
							window.Activate();
						})();
					}
				}
				var loading = new LoadingTask(activateTask, element);
				lv.Source = loading;
			}

			if (activate)
			{
				// Activate immediately to show the splash screen
				window.Activate();
			}
		}

		public void PreInitialize(FrameworkElement element, IApplicationBuilder builder)
		{
			if (element is ExtendedSplashScreen splash)
			{
				splash.Initialize(builder.Window, builder.Arguments);
			}
		}
	}

	public static IApplicationBuilder UseToolkitNavigation(this IApplicationBuilder builder)
	{
		builder.Properties.Add(typeof(IRootViewInitializer), new ToolkitViewInitializer());
		return builder.Configure(host => host.UseToolkitNavigation());
	}
}
