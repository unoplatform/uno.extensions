using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.ViewModels;
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

namespace ExtensionsSampleApp.UnoCommerce
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page, IViewModelStop, IInjectable<INavigator>
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private INavigator navigator;
        public void Inject(INavigator entity)
        {
            navigator = entity;
        }

        public async Task<bool> Stop(NavigationRequest request)
        {
            await navigator.NavigateToRouteAsync(this, "./Authenticating");
            await Task.Delay(2000);
            await navigator.NavigateToRouteAsync(this, "./Default");
            return !string.IsNullOrWhiteSpace(UsernameText.Text);
        }
    }
}
