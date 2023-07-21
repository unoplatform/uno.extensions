using Microsoft.Extensions.Logging;
using MauiControlsExternal;
using Telerik.Maui.Controls;
 using Telerik.Maui.Controls.Compatibility;
namespace MauiApp1;
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseTelerik()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.UseCustomLibrary()
			.UseTelerikControls();

#if DEBUG
		//builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
