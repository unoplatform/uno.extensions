using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ExtensionsSampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThirdPage : Page
    {
        public ThirdPage()
        {
            this.InitializeComponent();
        }

        private void GoBackNavigateToPreviousViewClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateToPreviousView(this);

        }

        private void GoToFourthPageRemoveOnePageNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.Navigate(new NavigationRequest(this, new NavigationRoute(new Uri("../FourthPage", UriKind.Relative))));
        }

        private void GoToFourthPageRemoveTwoPagesNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.Navigate(new NavigationRequest(this, new NavigationRoute(new Uri("../../FourthPage", UriKind.Relative))));
        }


        private void NextPageClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateToView<FourthPage>(this);
        }
    }
}
