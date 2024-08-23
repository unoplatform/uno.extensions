namespace Uno.Extensions.Navigation.UI.Controls;

/// <summary>
/// Wrapper for a <see cref="Frame"/> that can be used when navigating to a page to
/// make it easy to do subsequent forward/backward navigation.
/// </summary>
public sealed partial class FrameView : UserControl
{
	/// <summary>
	/// Constructor for the FrameView.
	/// </summary>
	public FrameView()
	{
		InitializeComponent();

		// Prevent inheritance of DataContext from Parent to avoid propagation to Children
		DataContext = null;
	}

	/// <summary>
	/// Returns the Navigator for the Frame region
	/// </summary>
	public INavigator? Navigator => NavigationFrame.Navigator();
}
