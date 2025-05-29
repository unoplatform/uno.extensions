using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;

namespace Uno.Extensions.Maui.Extensibility
{
	internal interface IMauiInitExtension
	{
		void Initialize(IApplication iApp) => throw new PlatformNotSupportedException();

		MauiAppBuilder RegisterPlatformServices(MauiAppBuilder builder, Application app) => throw new PlatformNotSupportedException();

		void InitializeMauiEmbeddingApp(MauiApp mauiApp, Application app) => throw new PlatformNotSupportedException();

		MauiApp BuildMauiApp(MauiAppBuilder builder, Application app, Microsoft.UI.Xaml.Window window) => throw new PlatformNotSupportedException();
	}
}
