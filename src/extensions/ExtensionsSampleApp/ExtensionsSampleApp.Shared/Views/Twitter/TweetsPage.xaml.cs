using ExtensionsSampleApp.ViewModels;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views.Twitter
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TweetsPage : Page, IInjectable<INavigationService>
    {
        private INavigationService Navigation { get; set; }

        public void Inject(INavigationService entity)
        {
            Navigation = entity;
        }

        public TweetsPage()
        {
            this.InitializeComponent();
        }


        public void TweetSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Navigation.NavigateForDataAsync(this, (sender as ListView).SelectedItem as Tweet);
        }
    }
}
