namespace Uno.Extensions.Maui.Platform;

/// <summary>
/// Provides a base Application class for Maui Embedding
/// </summary>
public partial class EmbeddingApplication
{
	static EmbeddingApplication()
	{
		// Forcing hot reload to false to prevent exceptions being raised
		Microsoft.Maui.HotReload.MauiHotReloadHelper.IsEnabled = false;
	}
}
