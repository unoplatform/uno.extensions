namespace Uno.Extensions.Maui.Internals;

internal sealed record EmbeddingApp(IServiceProvider Services, IApplication Application) : IPlatformApplication;
