namespace Uno.Extensions.Navigation.UI.Controls;

public sealed partial class FrameView : UserControl
{
	public FrameView()
	{
		this.InitializeComponent();
	}

	public INavigator? Navigator => NavigationFrame.Navigator();
}
