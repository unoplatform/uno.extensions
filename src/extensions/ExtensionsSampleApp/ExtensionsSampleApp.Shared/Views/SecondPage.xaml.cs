using System;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SecondPage : IInjectable<INavigator>
    {
        private INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public SecondPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ParametersText.Text = e.Parameter.ParseParameter();
        }

        private void GoBackFrameClick(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
        private void GoBackNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new Uri("../-", UriKind.Relative).AsRoute()));
        }

        private void GoBackNavigatePreviousViewAsyncClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigatePreviousWithResultAsync<Widget>(this, data: new Widget());
        }

        private async void NextPageNavigateViewAsyncRequestDataClick(object sender, RoutedEventArgs e)
        {
            var response = await Navigation.NavigateViewForResultAsync<ThirdPage, Widget>(this);
            if (response?.Result != null)
            {
                await response.Result;
            }
        }

        private void NextPageNavigateViewAsyncWithDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateViewAsync<ThirdPage>(this, data: new Widget());
        }

        private void NextPageNavigateViewAsyncWithQueryAndDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new Uri(typeof(ThirdPage).Name + "?arg1=val1&arg2=val2", UriKind.Relative).AsRoute(new Widget())));
        }
    }

    public class Widget
    {
        private static int widgetId = 0;
        public string Title { get; } = widgetId++ + " This is a widget to test sending data in navigation - " + DateTimeOffset.Now.ToString();

        public override string ToString()
        {
            return $"Widget - {Title}";
        }
    }
}
