using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Extensions.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NavigationViewPage : Page, IInjectable<INavigationService>
    {
        private INavigationService Navigation { get; set; }

        public void Inject(INavigationService entity)
        {
            Navigation = entity;
        }

        public NavigationViewPage()
        {
            this.InitializeComponent();
        }

        private void NavView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var navPath = Uno.Extensions.Navigation.Controls.Navigation.GetPath(args.InvokedItemContainer as FrameworkElement);
            Navigation.NavigateByPathAsync(this, navPath);
        }
    }
}
