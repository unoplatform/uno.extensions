//using Chinook.SectionsNavigation;
//using Nventive.ExtendedSplashScreen;
using Windows.ApplicationModel.Activation;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate
{
    public sealed partial class Shell : UserControl
    {
        public Shell(IActivatedEventArgs e)
        {
            this.InitializeComponent();

            Instance = this;

////-:cnd:noEmit
//#if WINDOWS_UWP
////+:cnd:noEmit
//            AppExtendedSplashScreen.SplashScreen = e?.SplashScreen;
////-:cnd:noEmit
//#endif
////+:cnd:noEmit
        }

        public static Shell Instance { get; private set; }

        //public IExtendedSplashScreen ExtendedSplashScreen => this.AppExtendedSplashScreen;

        //public MultiFrame NavigationMultiFrame => this.RootNavigationMultiFrame;
        public Frame NavigationFrame => this.RootFrame;
    }
}
