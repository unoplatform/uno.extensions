using ExtensionsSampleApp.ViewModels;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TabbedPage : INavigationAware
    {
        public TabbedPage()
        {
            InitializeComponent();
        }

        public INavigationService Navigation { get; set; }

        private void NavigateToDoc0Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Navigation.ChildNavigation().NavigateToViewModel<TabDoc0ViewModel>(this);
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToView<SimpleContentDialog, ContentDialogResult>(this);
            var response = await navresult.Result;
        }
    }
}
