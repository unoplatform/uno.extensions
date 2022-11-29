//-:cnd:noEmit
global using System;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.UI.Xaml;
//+:cnd:noEmit
#if markup
global using Microsoft.UI.Xaml.Automation;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Data;
#else
global using Microsoft.UI.Xaml.Controls;
#endif
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Navigation;
global using MyExtensionsApp.Business.Models;
global using MyExtensionsApp.Presentation;
global using MyExtensionsApp.Views;
global using Uno.Extensions;
global using Uno.Extensions.Configuration;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Localization;
global using Uno.Extensions.Navigation;
#if markup
global using Uno.Material;
global using Uno.Toolkit.UI;
global using Uno.Toolkit.UI.Material;
#else
global using Uno.Toolkit.UI;
#endif
global using Windows.ApplicationModel;
global using Application = Microsoft.UI.Xaml.Application;
#if markup
global using Color = Windows.UI.Color;
#endif
//-:cnd:noEmit
