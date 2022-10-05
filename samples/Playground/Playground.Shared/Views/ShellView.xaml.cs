using Uno.Toolkit.UI;

namespace Playground.Views;

public sealed partial class ShellView : UserControl
{
	public ExtendedSplashScreen SplashScreen => Splash;
	public ShellView()
	{
		this.InitializeComponent();
	}
}
