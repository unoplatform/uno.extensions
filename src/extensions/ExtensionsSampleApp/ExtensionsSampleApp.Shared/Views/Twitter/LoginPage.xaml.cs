using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace ExtensionsSampleApp.Views.Twitter
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page, IViewModelStop
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        public async Task<bool> Stop(NavigationRequest request)
        {
            VisualStateManager.GoToState(this, "Authenticating",true);
            await Task.Delay(2000);
            VisualStateManager.GoToState(this, "Default", true);
            return UsernameText.Text=="User1";
        }
    }
}
