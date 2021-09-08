using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SecondPage : INavigationAware
    {
        public INavigationService Navigation { get; set; }

        public SecondPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ParametersText.Text = e.Parameter.ParseParameter();
        }
        private void GoBackNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            Navigation.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri("..", UriKind.Relative))));
        }

        private void GoBackNavigateToPreviousViewClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToPreviousView(this, new Widget());
        }

        private void NextPageNavigateToViewWithDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToView<ThirdPage>(this, new Widget());
        }

        private void NextPageNavigateToViewWithQueryAndDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri(typeof(ThirdPage).Name + "?arg1=val1&arg2=val2", UriKind.Relative), new Widget())));
        }
    }

    public class Widget
    {
        public string Title { get; } = "This is a widget to test sending data in navigation - " + DateTimeOffset.Now.ToString();

        public override string ToString()
        {
            return $"Widget - {Title}";
        }
    }
}
