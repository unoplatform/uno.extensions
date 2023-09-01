using Syncfusion.Maui.Core.Hosting;

namespace MauiEmbedding.MauiControls;

public static class MauiAppBuilderExtensions
{
	public static MauiAppBuilder UseCustomLibrary(this MauiAppBuilder builder)
	{
		CustomEntry.Init();
		builder.ConfigureEssentials();
		builder.Services.AddSingleton(ctx => Vibration.Default);
		builder.ConfigureSyncfusionCore();
		return builder;
	}
}
