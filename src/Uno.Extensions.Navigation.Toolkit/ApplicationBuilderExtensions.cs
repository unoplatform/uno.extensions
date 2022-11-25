namespace Uno.Extensions;

public static class ApplicationBuilderExtensions
{
	private struct ToolkitViewInitializer : IRootViewInitializer
	{
		public ContentControl CreateDefaultView() => new LoadingView();

		public void InitializeViewHost(FrameworkElement element, Task loadingTask)
		{
			if (element is LoadingView loadingView)
			{
				loadingView.Source = new LoadingTask(loadingTask, element);
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
