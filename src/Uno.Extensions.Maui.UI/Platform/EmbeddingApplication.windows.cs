namespace Uno.Extensions.Maui.Platform;

/// <summary>
/// Provides a base Application class for Maui Embedding
/// </summary>
public partial class EmbeddingApplication : MauiWinUIApplication
{
	/// <inheritdoc />
	protected sealed override MauiApp CreateMauiApp() => throw new NotImplementedException();

	/// <inheritdoc />
	protected override void OnLaunched(LaunchActivatedEventArgs args) { }

	internal void InitializeApplication(IServiceProvider services, IApplication application)
	{
		Services = services;
		Application = application;
		IPlatformApplication.Current = this;
	}
}
