using ExtensionsSampleApp.ViewModels;
using ExtensionsSampleApp.ViewModels.Twitter;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views.Twitter
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TweetsPage : Page, IInjectable<INavigator>, IInjectable<IRouteMappings>
    {
        private INavigator Navigation { get; set; }

        private IRouteMappings Mappings { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public void Inject(IRouteMappings mappings)
        {
            Mappings = mappings;
        }

        public TweetsPage()
        {
            this.InitializeComponent();
        }


        public void TweetSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Navigation.NavigateToDataAsync(Mappings, this, (sender as ListView).SelectedItem as Tweet);
        }
    }
}
