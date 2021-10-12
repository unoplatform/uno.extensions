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
    public sealed partial class TabbedPage : IInjectable<INavigationService>
    {
        private INavigationService Navigation { get; set; }

        public void Inject(INavigationService entity)
        {
            Navigation = entity;
        }

        public TabbedPage()
        {
            InitializeComponent();
        }

        private void NavigateToDoc0Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Navigation.NavigateToViewModelAsync<TabDoc0ViewModel>(this,RouteConstants.Schemes.Current);
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.NavigateToViewAsync<SimpleContentDialog, ContentDialogResult>(this);
            var response = await navresult.Result;
        }
    }
}
