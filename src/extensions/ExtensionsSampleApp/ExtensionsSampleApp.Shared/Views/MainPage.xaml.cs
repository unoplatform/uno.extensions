using CommunityToolkit.Mvvm.DependencyInjection;
using ExtensionsSampleApp.ViewModels;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage 
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void NextPageNavigateToViewClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            await nav.NavigateToView<SecondPage>(this);
        }

        private void NextPageNavigateToViewModelClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateToViewModel<SecondViewModel>(this);
        }

        private void NextPageNavigateForDataClick(object sender, RoutedEventArgs e)
        {
            var nav = Ioc.Default.GetService<INavigationService>();
            nav.NavigateForData(this, new Widget());
        }
    }
}
