using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NavigationViewGridVisibilityPage : Page, IInjectable<INavigator>
    {
        private INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public NavigationViewGridVisibilityPage()
        {
            this.InitializeComponent();
        }

        private void NavView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var navPath = Uno.Extensions.Navigation.Controls.Navigation.GetRoute(args.InvokedItemContainer as FrameworkElement);
            Navigation.NavigateByPathAsync(this, navPath);
        }
    }
}
