global using System;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using System.Threading;

#if WINUI
	global using Microsoft.UI.Xaml;
	global using Microsoft.UI.Dispatching;
#else
global using Windows.System;
global using Windows.UI.Core;
	global using Windows.UI.Xaml;
#endif
