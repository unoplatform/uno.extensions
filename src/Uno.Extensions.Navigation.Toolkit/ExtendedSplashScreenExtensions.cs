namespace Uno.Extensions;

#if WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

public static class ExtendedSplashScreenExtensions
{
	public static void Initialize(this ExtendedSplashScreen splash, Window window, LaunchActivatedEventArgs args)
	{
		splash.Window = window;
#if WINDOWS_UWP
		splash.SplashScreen = args?.SplashScreen;
#elif WINDOWS
		splash.SplashScreen = args?.UWPLaunchActivatedEventArgs.SplashScreen;
#endif
	}
}
