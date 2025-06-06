global using System;
global using System.Globalization;
global using System.Linq;
global using System.Reflection;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Maui;
global using Microsoft.Maui.ApplicationModel;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Embedding;
global using Microsoft.Maui.Hosting;
global using Microsoft.Maui.Platform;
global using Uno.Extensions;
global using Uno.Extensions.Maui.Extensibility;
global using Uno.Extensions.Maui.Internals;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Markup;
global using Microsoft.UI.Xaml.Media;

// Where there's a name conflict, alias the WinUI type with same name
global using Application = Microsoft.UI.Xaml.Application;
global using Color = Windows.UI.Color;
global using ContentPropertyAttribute = Microsoft.UI.Xaml.Markup.ContentPropertyAttribute;
global using IValueConverter = Microsoft.UI.Xaml.Data.IValueConverter;
global using ResourceDictionary = Microsoft.UI.Xaml.ResourceDictionary;
global using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
global using Thickness = Microsoft.UI.Xaml.Thickness;

// Where there's a name conflict, alias Maui types with Maui prefix
global using IMauiContext = Microsoft.Maui.IMauiContext;
global using IMauiValueConverter = Microsoft.Maui.Controls.IValueConverter;
global using MauiApp = Microsoft.Maui.Hosting.MauiApp;
global using MauiAppBuilder = Microsoft.Maui.Hosting.MauiAppBuilder;
global using MauiApplication = Microsoft.Maui.Controls.Application;
global using MauiBindingMode = Microsoft.Maui.Controls.BindingMode;
global using MauiContext = Microsoft.Maui.MauiContext;
global using MauiControlsBinding = Microsoft.Maui.Controls.Binding;
global using MauiGraphicsColor = Microsoft.Maui.Graphics.Color;
global using MauiResourceDictionary = Microsoft.Maui.Controls.ResourceDictionary;
global using MauiSolidColorBrush = Microsoft.Maui.Controls.SolidColorBrush;
global using MauiView = Microsoft.Maui.Controls.View;
