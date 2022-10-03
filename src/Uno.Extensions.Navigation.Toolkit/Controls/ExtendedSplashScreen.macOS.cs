#if __MACOS__
using System;
using Windows.ApplicationModel.Activation;
#if WINUI
using Microsoft.UI.Xaml;
#endif

namespace Nventive.ExtendedSplashScreen
{
	public partial class ExtendedSplashScreen
	{
		private FrameworkElement GetNativeSplashScreen(SplashScreen splashScreen)
		{
			// ExtendedSplashscreen is not implemented on macOS.
			return null;
		}
	}
}
#endif
