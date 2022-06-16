global using System;
global using System.Collections.Generic;
global using System.Linq;
//global using Uno.Extensions.Navigation;
//global using Uno.Extensions.Navigation.Navigators;
//global using Uno.Extensions.Navigation.Regions;
//global using Uno.Extensions.Navigation.UI;
//global using Uno.Extensions.Navigation.UI.Controls;
//global using Uno.Extensions.Hosting;
global using System.Runtime.CompilerServices;
global using System.Text.Json;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
global using Microsoft.Identity.Client;
global using Uno.Extensions.Hosting;
global using Uno.UI.MSAL;
global using Windows.Security.Authentication.Web;


#if WINUI
#else
global using Windows.System;
	global using Windows.UI.Xaml;
	global using Windows.UI.Xaml.Controls;
	global using Windows.UI.Xaml.Controls.Primitives;
	global using Windows.UI.Xaml.Input;
	global using Windows.UI.Xaml.Navigation;
	global using Windows.UI.Xaml.Markup;
	global using Windows.UI.Xaml.Data;
	global using Windows.UI.Xaml.Media;
#endif
