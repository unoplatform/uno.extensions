namespace Uno.Extensions.Navigation;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IRootViewInitializer
{
	/// <summary>
	/// Creates a default navigation root container
	/// </summary>
	/// <returns></returns>
	ContentControl CreateDefaultView();

	/// <summary>
	/// Perform any initialization required before the Window is Activated
	/// </summary>
	/// <param name="element"></param>
	/// <param name="builder"></param>
	void PreInitialize(FrameworkElement element, IApplicationBuilder builder);

	/// <summary>
	/// Provide a startup delegate that can wait for the host startup
	/// </summary>
	/// <param name="window"></param>
	/// <param name="element"></param>
	/// <param name="loadingTask"></param>
	/// <param name="doNotActivate"></param>
	void InitializeViewHost(Window window, FrameworkElement element, Task loadingTask, bool doNotActivate = false);
}
