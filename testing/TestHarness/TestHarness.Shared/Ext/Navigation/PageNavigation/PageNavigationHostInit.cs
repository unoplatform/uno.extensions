using Microsoft.Extensions.Hosting;

namespace TestHarness.Ext.Navigation.PageNavigation;

public class PageNavigationHostInit : IHostInitialization
{
	public IHost InitializeHost()
	{

		return UnoHost
				.CreateDefaultBuilder()
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif

				// Add platform specific log providers
				.UseLogging(configure: (context, logBuilder) =>
				{
					var host = context.HostingEnvironment;
					// Configure log levels for different categories of logging
					logBuilder
							.SetMinimumLevel(host.IsDevelopment() ? LogLevel.Trace : LogLevel.Information)
							.XamlLogLevel(LogLevel.Information)
							.XamlLayoutLogLevel(LogLevel.Information);
				})


				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)
				// Add navigation support for toolkit controls such as TabBar and NavigationView
				.UseToolkitNavigation()

				.Build(enableUnoLogging: true);
	}


	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		var confirmDialog = new MessageDialogViewMap(
				Content: "Confirm this message?",
				Title: "Confirm?",
				DelayUserInput: true,
				DefaultButtonIndex: 1,
				Buttons: new DialogAction[]
				{
								new(Label: "Yeh!",Id:"Y"),
								new(Label: "Nah", Id:"N")
				}
			);

		views.Register(
			confirmDialog
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
			Nested: new[]
			{
					new RouteMap("Confirm", View: confirmDialog),
			}));
	}
}


