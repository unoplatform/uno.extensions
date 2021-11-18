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
    public sealed partial class TabbedPage : IInjectable<INavigator>
    { 
        private INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public TabbedPage()
        {
            InitializeComponent();
        }

        private void NavigateDoc0Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Navigation.NavigateViewModelAsync<TabDoc0ViewModel>(this, Schemes.Nested);
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.NavigateViewForResultAsync<SimpleContentDialog, ContentDialogResult>(this, Schemes.Dialog);
            var response = await navresult.Result;
        }

        private void Doc2GridLoaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
