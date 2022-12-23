//-:cnd:noEmit
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.UI.Xaml;
//+:cnd:noEmit
#if use-csharp-markup
global using Microsoft.UI.Xaml.Automation;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Data;
#else
global using Microsoft.UI.Xaml.Controls;
#endif
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Navigation;
#if (!blank-app-template)
#if (use-configuration)
global using Microsoft.Extensions.Options;
#endif
global using MyExtensionsApp.Business.Models;
global using MyExtensionsApp.Presentation;
#if (use-http)
global using MyExtensionsApp.Services;
global using Uno.Extensions.Http;
#endif
global using Uno.Extensions.Navigation;
#if (use-http)
global using Refit;
#endif
global using Uno.Extensions;
#if use-configuration
global using Uno.Extensions.Configuration;
#endif
global using Uno.Extensions.Hosting;
#if use-localization
global using Uno.Extensions.Localization;
#endif
#if use-logging
global using Uno.Extensions.Logging;
#endif
#if use-csharp-markup
global using Uno.Material;
global using Uno.Themes.Markup;
global using Uno.Toolkit.UI;
global using Uno.Toolkit.UI.Material;
#else
global using Uno.Toolkit.UI;
#if use-csharp-markup
global using Button = Microsoft.UI.Xaml.Controls.Button;
global using Color = Windows.UI.Color;
#endif
#endif
#endif
global using Windows.ApplicationModel;
//-:cnd:noEmit
