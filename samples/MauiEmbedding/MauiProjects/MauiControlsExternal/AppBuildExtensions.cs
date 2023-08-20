using Syncfusion.Maui.Core.Hosting;

namespace MauiControlsExternal;

public static class MauiAppBuilderExtensions
{
	public static MauiAppBuilder UseCustomLibrary(this MauiAppBuilder builder)
	{
		CustomEntry.Init();
		builder.ConfigureSyncfusionCore();
		return builder;
	}
}
