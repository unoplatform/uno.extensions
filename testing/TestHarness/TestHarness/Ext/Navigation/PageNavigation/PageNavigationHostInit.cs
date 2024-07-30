using System.Collections.Immutable;

namespace TestHarness;

public class PageNavigationHostInit : BaseHostInitialization
{
	protected MessageDialogViewMap ConfirmDialog { get; }=
		new MessageDialogViewMap(
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
	protected override IHostBuilder Configuration(IHostBuilder builder)
	{
		return builder.UseConfiguration(configure: builder =>
		{
			return builder.Section<PageNavigationSettings>();
		});
	}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		

		views.Register(
			ConfirmDialog
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
			Nested: new[]
			{
					new RouteMap("Confirm", View: ConfirmDialog),
			}));
	}
}

public record PageNavigationSettings
{
	// Make sure you initialise the value
	public IImmutableList<string> PagesVisited { get; set; } = ImmutableList<string>.Empty;
}


[JsonSerializable(typeof(PageNavigationSettings))]
internal partial class PageNavigationSettingsContext : JsonSerializerContext
{ }

