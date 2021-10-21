using ExtensionsSampleApp.ViewModels;
using ExtensionsSampleApp.ViewModels.Twitter;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ExtensionsSampleApp.Views.Twitter
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotificationsPage : Page, IInjectable<INavigator>, IInjectable<IRouteMappings>
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

        public NotificationsPage()
        {
            this.InitializeComponent();
        }


        public void TweetSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Navigation.NavigateToDataAsync(Mappings, this, (sender as ListView).SelectedItem as Tweet);
        }

    }
}
