namespace Uno.Extensions;

/// <summary>
/// Extension methods on <see cref="Window" />.
/// </summary>
public static class WindowExtensions
{
	/// <summary>
	/// Activates the window when the content is ready
	/// </summary>
	/// <param name="window">The <see cref="Window" /> to activate</param>
	public static void ActivateWhenReady(this Window window)
	{

		if (window.Content is FrameworkElement content)
		{
			if (content.IsLoaded)
			{
				// Fallback to make sure the window is activated
				window.Activate();
			}
			else
			{
				content.Loaded += (_, _) => window.Activate();
				content.SizeChanged += (_, _) => window.Activate();
				content.Loading += (_, _) => window.Activate();
			}
		}
		else
		{
			window.Activate();
		}
	}
}
