global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;
global using CommunityToolkit.Mvvm.ComponentModel;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using TestHarness.Ext.Navigation.MessageDialog;
global using TestHarness.Ext.Navigation.PageNavigation;
global using TestHarness.Models;
global using Uno.Extensions;
global using Uno.Extensions.Authentication;
global using Uno.Extensions.Authentication.Custom;
global using Uno.Extensions.Authentication.Handlers;
global using Uno.Extensions.Authentication.MSAL;
global using Uno.Extensions.Configuration;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Http;
global using Uno.Extensions.Http.Refit;
global using Uno.Extensions.Localization;
global using Uno.Extensions.Logging;
global using Uno.Extensions.Navigation;
global using Uno.Extensions.Navigation.Regions;
global using Uno.Extensions.Navigation.Toolkit;
global using Uno.Extensions.Navigation.UI;
global using Uno.Extensions.Serialization;
global using Uno.Extensions.Serialization.Refit;
global using Windows.ApplicationModel;
global using Windows.ApplicationModel.Activation;

global using System.Text.Json.Serialization;
global using Refit;
global using Uno.Extensions.Authentication.Oidc;


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
global using Window = Microsoft.UI.Xaml.Window;
global using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
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
global using Window = Windows.UI.Xaml.Window;
global using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
global using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
#endif


