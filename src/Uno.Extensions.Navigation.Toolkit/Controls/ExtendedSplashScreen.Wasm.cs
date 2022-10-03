#if __WASM__
using System;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Activation;
#if WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif

namespace Nventive.ExtendedSplashScreen
{
	public partial class ExtendedSplashScreen
	{
		private FrameworkElement GetNativeSplashScreen(SplashScreen splashScreen)
		{
			// ExtendedSplashscreen is not implemented on WASM.
			return null;
		}
	}
}
#endif
