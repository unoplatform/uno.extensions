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

            Loaded += TabbedPage_Loaded;
            Unloaded += TabbedPage_Unloaded;
        }

        private void TabbedPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationManager>();
            nav.DeactivateAdapter(adapter);
        }

        private INavigationService adapter;
        private void TabbedPage_Loaded(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationManager>();
            adapter = nav.ActivateAdapter(Tabs);
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
        private void ThirdPageClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateToView<ThirdPage>(this);

        }
        private void SecondPageClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateToView<SecondPage>(this);
        }
        private void FourthPageClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateToView<FourthPage>(this);
        }

        private INavigationService innerFrameAdapter;
        private void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationManager>();
            if (e.AddedItems?.FirstOrDefault() == doc2)
            {
                nav.ActivateAdapter(InnerFrame);
            }
            else if(innerFrameAdapter != null)
                {
                nav.DeactivateAdapter(innerFrameAdapter);
            }
        }
    }
}
