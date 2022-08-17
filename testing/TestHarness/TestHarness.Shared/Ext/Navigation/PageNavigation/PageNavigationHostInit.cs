using System.Collections.Immutable;

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

				.UseConfiguration(configure: builder =>
				{
					return builder.Section<PageNavigationSettings>();
				})

				// Add platform specific log providers
				.UseLogging(configure: (context, logBuilder) =>
				{
					var host = context.HostingEnvironment;
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(host.IsDevelopment() ? LogLevel.Warning : LogLevel.Information);
				})

				// Enable navigation, including registering views and viewmodels
				.UseNavigation(RegisterRoutes)

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

public record PageNavigationSettings
{
	// This doesn't work, use T[] instead
	//public ImmutableList<string> PagesVisited { get; init; } = ImmutableList<string>.Empty;
	public string[] PagesVisited { get; init; } = Array.Empty<string>();
}


