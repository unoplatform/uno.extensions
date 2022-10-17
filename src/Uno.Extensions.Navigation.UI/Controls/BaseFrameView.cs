namespace Uno.Extensions.Navigation.UI.Controls;

public abstract partial class BaseFrameView : UserControl
{
	public abstract INavigator? Navigator { get; }

	public abstract Frame NavigationFrame { get; }

}
