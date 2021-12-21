using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Playground.ViewModels;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Playground.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FourthPage : Page, IInjectable<INavigator>
    {
		public FourthViewModel ViewModel { get; private set; }

		private INavigator Navigator { get; set; }
        public FourthPage()
        {
            this.InitializeComponent();

			DataContextChanged += FourthPage_DataContextChanged;
        }

		private void FourthPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as FourthViewModel;	
		}

		private void FifthPageClick(object sender, RoutedEventArgs args)
		{
			Navigator.NavigateViewAsync<FifthPage>(this, Schemes.Nested);
		}

		public void Inject(INavigator entity)
		{
			Navigator = entity;
		}
	}
}
