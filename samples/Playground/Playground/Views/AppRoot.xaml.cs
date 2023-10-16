using Uno.Toolkit.UI;

namespace Playground.Views;

public sealed partial class AppRoot : UserControl, IContentControlProvider
{
	public ExtendedSplashScreen SplashScreen => Splash;
	public AppRoot()
	{
		this.InitializeComponent();
	}

	public ContentControl ContentControl => Splash;
}
