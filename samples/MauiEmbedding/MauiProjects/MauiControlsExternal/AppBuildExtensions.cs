namespace MauiControlsExternal;
public static class MauiAppBuilderExtensions
{
	public static MauiAppBuilder UseCustomLibrary(this MauiAppBuilder builder)
	{
		CustomEntry.Init();

		return builder;
	}
}
