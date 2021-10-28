using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ExtensionsSampleApp.ViewModels;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{

    public sealed partial class FourthPage: IInjectable<INavigator>
    {
        private INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public FourthPage()
        {
            InitializeComponent();
        }

        private async void GoBackClick(object sender, RoutedEventArgs e)
        {
            await Navigation.GoBack(this);
        }
    }
}
