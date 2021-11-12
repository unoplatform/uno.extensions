using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.Extensions;
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
using Commerce.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Commerce
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page, IInjectable<INavigator>
    {
        public LoginPage()
        {
            this.InitializeComponent();
		}

        private INavigator navigator;
        public void Inject(INavigator entity)
        {
            navigator = entity;
			DataContext = VM = new LoginViewModel.BindableLoginViewModel(navigator);
		}

		public LoginViewModel.BindableLoginViewModel VM { get; private set; }

        public async Task<bool> Stop(NavigationRequest request)
        {
            VisualStateManager.GoToState(this, "Authenticating", true);
            await Task.Delay(2000);
            VisualStateManager.GoToState(this, "Default", true);
            return !string.IsNullOrWhiteSpace(UsernameText.Text);
        }
    }
}
