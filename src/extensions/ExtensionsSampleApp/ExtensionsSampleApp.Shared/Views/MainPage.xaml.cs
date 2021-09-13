using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using ExtensionsSampleApp.ViewModels;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Adapters;
using Uno.Extensions.Navigation.Controls;
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
            Navigation.Navigate(new NavigationRequest(sender, new NavigationRoute(new Uri("SecondPage", UriKind.Relative))));
        }

        private async void NextPageNavigateToViewClick(object sender, RoutedEventArgs e)
        {
            await Navigation.NavigateToView<SecondPage>(this);
        }

        private void NextPageNavigateToViewModelClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToViewModel<SecondViewModel>(this);
        }

        private void NextPageNavigateForDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateForData(this, new Widget());
        }

        private async void NextPageRequestResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToViewModel<SecondViewModel, Widget>(this);
            var response = await navresult.Result;
        }

        private async void NextPageRequestResponseWithTimeoutClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToViewModel<SecondViewModel, Widget>(this);
            Task.Run(() => Task.Delay(10000)).ConfigureAwait(true).GetAwaiter().OnCompleted(() => navresult.CancellationSource.Cancel());
            var response = await navresult.Result;
        }
        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToView<SimpleContentDialog, ContentDialogResult>(this);
            var response = await navresult.Result;
        }

        private async void ContentDialogWidgetResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToView<SimpleContentDialog, Widget>(this);
            var response = await navresult.Result;
        }

        private async void ContentDialogResultAndWidgetResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToView<SimpleContentDialog, ContentResult>(this);
            var response = await navresult.Result;
        }

        private async void MessageDialogClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.ShowMessageDialog(this, "Basic content", "Content Title", commands: new Windows.UI.Popups.UICommand("test", command => Debug.WriteLine("TEST")));
            var response = await navresult.Result;
        }

    }
}
