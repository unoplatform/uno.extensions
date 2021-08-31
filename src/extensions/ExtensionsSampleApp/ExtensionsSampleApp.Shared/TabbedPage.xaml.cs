using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ExtensionsSampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TabbedPage : Page
    {
        public TabbedPage()
        {
            this.InitializeComponent();
        }

        private void ActivateDoc0Click(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.Navigate(new NavigationRequest(this, new NavigationRoute(new Uri("doc0", UriKind.Relative))));

        }
        private void ActivateHomeClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.Navigate(new NavigationRequest(this, new NavigationRoute(new Uri("home", UriKind.Relative))));

        }
    }
}
