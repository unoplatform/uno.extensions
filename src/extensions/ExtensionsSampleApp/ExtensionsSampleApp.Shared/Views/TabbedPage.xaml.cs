using ExtensionsSampleApp.ViewModels;
using Uno.Extensions.Navigation;

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
    }
}
