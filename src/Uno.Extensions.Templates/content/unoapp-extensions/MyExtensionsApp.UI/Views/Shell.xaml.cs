using Uno.Toolkit.UI;

namespace MyExtensionsApp.Views;

public sealed partial class Shell : UserControl
{
	public LoadingView SplashScreen => Splash;
	public Shell()
	{
		this.InitializeComponent();
	}
}
