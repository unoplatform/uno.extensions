using System;
using System.Threading;
using System.Threading.Tasks;
using ExtensionsSampleApp.ViewModels;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : IInjectable<INavigator>
    {
        private INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public MainPage()
        {
            InitializeComponent();
        }

        private void NextPageNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new Uri("../SecondPage", UriKind.Relative).BuildRoute()));
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
            var navresult = await Navigation.NavigateToViewModelAsync<SecondViewModel, Widget>(this);
            var response = await navresult.Result;
        }

        private async void RequestDataResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.NavigateForResultDataAsync<Widget>(this);
            var response = await navresult.Result;
        }

        private async void NextPageRequestResponseWithTimeoutClick(object sender, RoutedEventArgs e)
        {
            var cancel = new CancellationTokenSource();
            var navresult = await Navigation.NavigateToViewModelAsync<SecondViewModel, Widget>(this, cancellation: cancel.Token);
            Task.Run(() => Task.Delay(10000)).ConfigureAwait(true).GetAwaiter().OnCompleted(() => cancel.Cancel());
            var response = await navresult.Result;
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.NavigateToViewAsync<SimpleContentDialog, ContentDialogResult>(this, Schemes.Dialog);
            var response = await navresult.Result;
        }

        private async void ContentDialogWidgetResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.NavigateToViewAsync<SimpleContentDialog, Widget>(this, Schemes.Dialog);
            var response = await navresult.Result;
        }

        //private async void ContentDialogResultAndWidgetResponseClick(object sender, RoutedEventArgs e)
        //{
        //    var navresult = await Navigation.NavigateToViewAsync<SimpleContentDialog, ContentResult>(this, Schemes.Dialog);
        //    var response = await navresult.Result;
        //}

        private async void MessageDialogClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.ShowMessageDialogAsync(this, "Basic content", "Content Title");//, commands: new Windows.UI.Popups.UICommand[] { new Windows.UI.Popups.UICommand("test", command => Debug.WriteLine("TEST")) });
            var response = await navresult.Result;
        }

        private async void ShowPopupManualClick(object sender, RoutedEventArgs e)
        {
#if __IOS__
            try
            {
                var navresult = await Navigation.ShowPickerAsync(this, new List<string> { "One", "Two", "Three", "Four" }, this.Resources["PickerTemplate"]);
                var result = await navresult.Result;
                var msgresult = await Navigation.ShowMessageDialogAsync(this, result, "Result");
            }
            catch (Exception ex)
            {
                var msgerror = await Navigation.ShowMessageDialogAsync(this, ex.StackTrace, "Error - " + ex.Message);

            }
#endif
        }
    }


}
