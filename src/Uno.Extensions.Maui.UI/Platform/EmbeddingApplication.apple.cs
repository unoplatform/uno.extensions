using Foundation;
using UIKit;

namespace Uno.Extensions.Maui.Platform;

/// <summary>
/// Provides a base Application class for Maui Embedding
/// </summary>
public class EmbeddingApplication : Application, IPlatformApplication
{
	private IServiceProvider _serviceProvider = default!;
	private IApplication _application = default!;

	IServiceProvider IPlatformApplication.Services => _serviceProvider;
	IApplication IPlatformApplication.Application => _application;

	internal void InitializeApplication(IServiceProvider services, IApplication application)
	{
		_serviceProvider = services;
		_application = application;

		// Hack: This is a workaround for https://github.com/dotnet/maui/pull/16803
		HackMauiUIApplicationDelegate.Initialize(services, application);
		IPlatformApplication.Current = this;
	}

	private class HackMauiUIApplicationDelegate : MauiUIApplicationDelegate
	{
		private HackMauiUIApplicationDelegate(IServiceProvider services, IApplication application)
		{
			Services = services;
			Application = application;
		}

		public static HackMauiUIApplicationDelegate Initialize(IServiceProvider services, IApplication application) =>
			new(services, application);
		protected override MauiApp CreateMauiApp() => throw new NotImplementedException();

		public override bool WillFinishLaunching(UIApplication application, NSDictionary launchOptions) => true;

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) => true;
	}
}
