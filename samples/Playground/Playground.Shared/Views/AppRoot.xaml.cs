using Uno.Toolkit.UI;

namespace Playground.Views;

public sealed partial class AppRoot : UserControl
{
	public ExtendedSplashScreen SplashScreen => Splash;
	public AppRoot()
	{
		this.InitializeComponent();
	}
}
