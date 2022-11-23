using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.Extensions;
using Uno.Extensions.Navigation.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Playground
{
	public sealed partial class ThemeSwitchPage : Page
	{
		private ThemeService ts;
		public ThemeSwitchPage()
		{
			this.InitializeComponent();
			var w = (Application.Current as App)?.Window;
			ts = new ThemeService(w, new Dispatcher(w));
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (ts.IsDarkMode) await ts.SetThemeAsync(false);
			else await ts.SetThemeAsync(true);
		}
	}
}
