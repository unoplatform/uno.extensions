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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ExtensionsSampleApp.Views
{
    public sealed partial class Content1 : UserControl, INavigationAware
    {
        public INavigationService Navigation { get; set; }

        public Content1()
        {
            this.InitializeComponent();
        }

        private async void ContentDialogResponseClick(object sender, RoutedEventArgs e)
        {
            var navresult = Navigation.NavigateToView<SimpleContentDialog, ContentDialogResult>(this);
            var response = await navresult.Result;
        }
    }
}
