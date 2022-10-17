namespace Uno.Extensions.Navigation.UI.Controls;

public sealed partial class FrameView : BaseFrameView
{
	public FrameView()
	{
		this.InitializeComponent();
	}

	public override INavigator? Navigator => NavFrame.Navigator();

	public override Frame NavigationFrame => NavFrame;
}
