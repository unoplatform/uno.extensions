global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.FileProviders;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Hosting.Internal;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Storage;
global using Uno.Extensions.Hosting.Internal;
global using Windows.Storage;
global using Windows.ApplicationModel.Core;

#if WINUI
	global using Microsoft.UI.Xaml;
	global using Microsoft.UI.Xaml.Controls;
	global using Window = Microsoft.UI.Xaml.Window;
	global using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
	global using Application = Microsoft.UI.Xaml.Application;
#else
	global using Windows.UI.Xaml;
	global using Windows.UI.Xaml.Controls;
	global using Window = Windows.UI.Xaml.Window;
	global using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
	global using Application = Windows.UI.Xaml.Application;
#endif

