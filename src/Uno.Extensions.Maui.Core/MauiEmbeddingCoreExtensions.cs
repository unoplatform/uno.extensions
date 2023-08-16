namespace Uno.Extensions.Maui;

/// <summary>
/// Provides core extensions for Maui Embedding
/// </summary>
public static class MauiEmbeddingCoreExtensions
{
	/// <summary>
	/// When providing a <see cref="ResourceDictionary"/> with this method, the resources will be provided by default
	/// for all Maui controls.
	/// </summary>
	/// <typeparam name="TResources"></typeparam>
	/// <param name="maui"></param>
	/// <returns></returns>
	public static MauiAppBuilder UseMauiEmbeddingResources<TResources>(this MauiAppBuilder maui)
		where TResources : ResourceDictionary, new()
	{
		maui.Services.AddSingleton(new MauiResourceProvider(new TResources()));
		return maui;
	}
}
