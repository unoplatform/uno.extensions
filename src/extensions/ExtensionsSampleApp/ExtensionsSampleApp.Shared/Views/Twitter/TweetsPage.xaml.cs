using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ExtensionsSampleApp.ViewModels;
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

namespace ExtensionsSampleApp.Views.Twitter
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TweetsPage : Page, INavigationAware
    {
        public INavigationService Navigation { get; set; }
        public TweetsPage()
        {
            this.InitializeComponent();
        }


        public void TweetSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            Navigation.NavigateForDataAsync(this, (sender as ListView).SelectedItem as Tweet);
        }
    }
}
