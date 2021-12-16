//-:cnd:noEmit
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Toolkit.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;

#if WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyExtensionsApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ProfilePage : Page
	{
		public ProfilePage()
		{
			this.InitializeComponent();

			this.Loaded += (s, e) =>
			{
				// Initialize the toggle to the current theme.
				darkModeToggle.IsEnabled = false;
				darkModeToggle.IsOn = SystemThemeHelper.IsRootInDarkMode(XamlRoot);
				darkModeToggle.IsEnabled = true;
			};
		}

		private void ToggleDarkMode()
		{
			if (darkModeToggle.IsEnabled)
			{
				SystemThemeHelper.SetRootTheme(XamlRoot, darkModeToggle.IsOn);
			}
		}
	}
}
