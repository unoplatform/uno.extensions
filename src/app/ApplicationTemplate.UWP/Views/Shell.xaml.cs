using Chinook.SectionsNavigation;
using Nventive.ExtendedSplashScreen;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;

namespace ApplicationTemplate
{
    public sealed partial class Shell : UserControl
    {
        public Shell(IActivatedEventArgs e)
        {
            this.InitializeComponent();

            Instance = this;

//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
            AppExtendedSplashScreen.SplashScreen = e?.SplashScreen;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
        }

        public static Shell Instance { get; private set; }

        public IExtendedSplashScreen ExtendedSplashScreen => this.AppExtendedSplashScreen;

        public MultiFrame NavigationMultiFrame => this.RootNavigationMultiFrame;
    }
}
