global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.Linq;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Windows.Foundation;
global using Windows.UI.Popups;
global using Uno.Extensions;
global using Uno.Extensions.Configuration;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Logging;
global using Uno.Extensions.Navigation;
global using Uno.Extensions.Navigation.Navigators;
global using Uno.Extensions.Navigation.Regions;
global using Uno.Extensions.Navigation.UI;
global using Uno.Extensions.Navigation.UI.Controls;

global using UICommand = Windows.UI.Popups.UICommand;

#if WINUI
global using Microsoft.UI.Dispatching;
	global using Microsoft.UI.Xaml;
	global using Microsoft.UI.Xaml.Controls;
	global using Microsoft.UI.Xaml.Controls.Primitives;
	global using Microsoft.UI.Xaml.Input;
	global using Microsoft.UI.Xaml.Navigation;
	global using Microsoft.UI.Xaml.Markup;
	global using Microsoft.UI.Xaml.Data;
	global using Microsoft.UI.Xaml.Media;
	global using Application = Microsoft.UI.Xaml.Application;
#else
	global using Windows.System;
	global using Windows.UI.Core;
	global using Windows.UI.Xaml;
	global using Windows.UI.Xaml.Controls;
	global using Windows.UI.Xaml.Controls.Primitives;
	global using Windows.UI.Xaml.Input;
	global using Windows.UI.Xaml.Navigation;
	global using Windows.UI.Xaml.Markup;
	global using Windows.UI.Xaml.Data;
	global using Windows.UI.Xaml.Media;
	global using Application = Windows.UI.Xaml.Application;
#endif
