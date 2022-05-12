//-:cnd:noEmit
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Windows.ApplicationModel;
global using Windows.ApplicationModel.Core;
global using Windows.Foundation;
global using Windows.UI.Popups;
global using Uno.Foundation;
global using Uno.Extensions;
global using Uno.Extensions.Configuration;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Navigation;
global using Uno.Extensions.Localization;
global using Uno.Extensions.Logging;
global using Uno.Extensions.Serialization;
global using Uno.Extensions.Navigation.Toolkit;
global using Uno.Extensions.Navigation.Regions;
global using MyExtensionsApp.Configuration;
global using MyExtensionsApp.ViewModels;
global using MyExtensionsApp.Views;

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
