global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Windows.Foundation;
global using Windows.UI.Popups;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Navigation;
global using Uno.Extensions.Navigation.Navigators;
global using Uno.Extensions.Navigation.Regions;
global using Uno.Extensions.Navigation.Toolkit;
global using Uno.Extensions.Navigation.Toolkit.Controls;
global using Uno.Extensions.Navigation.Toolkit.Navigators;
global using Uno.Extensions.Navigation.UI;
global using Uno.Toolkit.UI;

#if WINUI
	global using Microsoft.UI.Dispatching;
	global using Microsoft.UI.Xaml;
	global using Microsoft.UI.Xaml.Controls;
	global using Microsoft.UI.Xaml.Controls.Primitives;
	global using Microsoft.UI.Xaml.Navigation;
	global using Microsoft.UI.Xaml.Markup;
	global using Microsoft.UI.Xaml.Data;
	global using Microsoft.UI.Xaml.Media;
#else
	global using Windows.System;
	global using Windows.UI.Xaml;
	global using Windows.UI.Xaml.Controls;
	global using Windows.UI.Xaml.Controls.Primitives;
	global using Windows.UI.Xaml.Navigation;
	global using Windows.UI.Xaml.Markup;
	global using Windows.UI.Xaml.Data;
	global using Windows.UI.Xaml.Media;
#endif
