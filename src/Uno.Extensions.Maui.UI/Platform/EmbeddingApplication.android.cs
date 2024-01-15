namespace Uno.Extensions.Maui.Platform;

/// <summary>
/// Provides a base Application class for Maui Embedding
/// </summary>
public partial class EmbeddingApplication : Application, IPlatformApplication
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
		HackMauiApplication.Initialize(services, application);
		IPlatformApplication.Current = this;
	}

	private class HackMauiApplication : Microsoft.Maui.MauiApplication
	{
		private HackMauiApplication(IServiceProvider services, IApplication application)
			: base(0, default)
		{
			// TODO: Adjust code to remove this warning (https://github.com/unoplatform/uno.extensions/issues/2142)
#pragma warning disable CS0618 // Type or member is obsolete 
			Services = services;
			Application = application;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		protected override MauiApp CreateMauiApp() => throw new NotImplementedException();

		public override void OnCreate() { }

		public static HackMauiApplication Initialize(IServiceProvider services, IApplication application) =>
			new (services, application);
	}
}
