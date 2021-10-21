using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{
    public sealed partial class Content1 : UserControl, IInjectable<INavigator>
    {
        public INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public Content1()
        {
            this.InitializeComponent();
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = await Navigation.NavigateToViewForResultAsync<SimpleContentDialog, ContentDialogResult>(this);
            var response = await navresult.Result;
        }

    }
}
