using System.Diagnostics;
using System.Threading.Tasks;
using ApplicationTemplate.Views;
using Chinook.SectionsNavigation;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace ApplicationTemplate
{
	sealed partial class App : Windows.UI.Xaml.Application
	{
		public App()
		{
			Instance = this;

			Startup = new Startup();
			Startup.PreInitialize();

			InitializeComponent();

			ConfigureOrientation();
		}

		public Activity ShellActivity { get; } = new Activity(nameof(Shell));

		public static App Instance { get; private set; }

		public static Startup Startup { get; private set; }

		public Shell Shell { get; private set; }

		public MultiFrame NavigationMultiFrame => Shell?.NavigationMultiFrame;

		public Window CurrentWindow => Windows.UI.Xaml.Window.Current;

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			InitializeAndStart(args);
		}

		protected override void OnActivated(IActivatedEventArgs args)
		{
			// This is where your app launches if you use custom schemes, Universal Links, or Android App Links.
			InitializeAndStart(args);
		}

		private void InitializeAndStart(IActivatedEventArgs args)
		{
			Shell = CurrentWindow.Content as Shell;

			var isFirstLaunch = Shell == null;

			if (isFirstLaunch)
			{
				ConfigureViewSize();
				ConfigureStatusBar();

				Startup.Initialize();

#if (IncludeFirebaseAnalytics)
				ConfigureFirebase();
#endif

				ShellActivity.Start();

				CurrentWindow.Content = Shell = new Shell(args);

				ShellActivity.Stop();
			}

			CurrentWindow.Activate();

			_ = Task.Run(() => Startup.Start());
		}

		private void ConfigureOrientation()
		{
			DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
		}

		private void ConfigureViewSize()
		{
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
			ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
			ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 480));
//-:cnd:noEmit
#endif
//+:cnd:noEmit
		}

		private void ConfigureStatusBar()
		{
			var resources = Windows.UI.Xaml.Application.Current.Resources;

//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
			var hasStatusBar = false;
//-:cnd:noEmit
#else
//+:cnd:noEmit
			var hasStatusBar = true;
			Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ForegroundColor = Windows.UI.Colors.White;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

			var statusBarHeight = hasStatusBar ? Windows.UI.ViewManagement.StatusBar.GetForCurrentView().OccludedRect.Height : 0;

			resources.Add("StatusBarDouble", (double)statusBarHeight);
			resources.Add("StatusBarThickness", new Thickness(0, statusBarHeight, 0, 0));
			resources.Add("StatusBarGridLength", new GridLength(statusBarHeight, GridUnitType.Pixel));
		}

#if (IncludeFirebaseAnalytics)
		private void ConfigureFirebase()
		{
//-:cnd:noEmit
#if __IOS__
//+:cnd:noEmit
			// This is used to initalize firebase and crashlytics.
			Firebase.Core.App.Configure();
//-:cnd:noEmit
#endif
//+:cnd:noEmit
		}
#endif
	}
}
