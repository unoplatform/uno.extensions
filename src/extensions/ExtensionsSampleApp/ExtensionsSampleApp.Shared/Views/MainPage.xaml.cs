using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ExtensionsSampleApp.ViewModels;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Dialogs;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : INavigationAware
    {
        public INavigationService Navigation { get; set; }
        public MainPage()
        {
            InitializeComponent();
        }

        private void NextPageNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new NavigationRoute(new Uri("SecondPage", UriKind.Relative))));
        }

        private async void NextPageNavigateToViewAsyncClick(object sender, RoutedEventArgs e)
        {
            await Navigation.NavigateToViewAsync<SecondPage>(this);
        }

        private void NextPageNavigateToViewModelAsyncClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToViewModelAsync<SecondViewModel>(this);
        }

        private void NextPageNavigateForDataAsyncClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateForDataAsync(this, new Widget());
        }

        private async void NextPageRequestResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToViewModelAsync<SecondViewModel, Widget>(this);
            var response = await navresult.Result;
        }

        private async void NextPageRequestResponseWithTimeoutClick(object sender, RoutedEventArgs e)
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var navresult = Navigation.NavigateToViewModelAsync<SecondViewModel, Widget>(this,cancellation: cancel.Token);
            //Task.Run(() => Task.Delay(10000)).ConfigureAwait(true).GetAwaiter().OnCompleted(() => navresult.CancellationSource.Cancel());
            var response = await navresult.Result;
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToViewAsync<SimpleContentDialog, ContentDialogResult>(this);
            var response = await navresult.Result;
        }

        private async void ContentDialogWidgetResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToViewAsync<SimpleContentDialog, Widget>(this);
            var response = await navresult.Result;
        }

        private async void ContentDialogResultAndWidgetResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToViewAsync<SimpleContentDialog, ContentResult>(this);
            var response = await navresult.Result;
        }

        private async void MessageDialogClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.ShowMessageDialogAsync(this, "Basic content", "Content Title", commands: new Windows.UI.Popups.UICommand[] { new Windows.UI.Popups.UICommand("test", command => Debug.WriteLine("TEST")) });
            var response = await navresult.Result;
        }

    }
}
