global using System;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
global using Uno.Toolkit.UI;
global using Uno.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
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
	global using Microsoft.UI.Xaml;
	global using Windows.UI.Xaml;
#endif
