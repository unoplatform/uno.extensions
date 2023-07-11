namespace MauiControlsExternal;
public static class AppBuildExtensions
{
	public static MauiAppBuilder UseCustomLibrary(this MauiAppBuilder builder)
	{
		CustomEntry.Init();

		return builder;
	}
}
