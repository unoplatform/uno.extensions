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
            Navigation.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri("..", UriKind.Relative))));
        }

        private void GoBackNavigateToPreviousViewAsyncClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToPreviousViewAsync(this, data: new Widget());
        }

        private void NextPageNavigateToViewAsyncWithDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToViewAsync<ThirdPage>(this, data: new Widget());
        }

        private void NextPageNavigateToViewAsyncWithQueryAndDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri(typeof(ThirdPage).Name + "?arg1=val1&arg2=val2", UriKind.Relative), new Widget())));
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
