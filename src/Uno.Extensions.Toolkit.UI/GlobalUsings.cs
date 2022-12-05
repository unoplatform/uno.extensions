global using System;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Uno.Extensions.Configuration;
global using Uno.Extensions.Logging;
global using Uno.Extensions.Toolkit;
global using Uno.Toolkit.UI;

#if WINUI
	global using Microsoft.UI.Xaml;
#else
	global using Microsoft.UI.Xaml;
	global using Windows.UI.Xaml;
#endif
