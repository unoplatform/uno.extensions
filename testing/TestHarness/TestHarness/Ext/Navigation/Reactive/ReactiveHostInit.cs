namespace TestHarness.Ext.Navigation.Reactive;

public class ReactiveHostInit : BaseHostInitialization
{
	protected override IHostBuilder Navigation(IHostBuilder builder)
	{
		return builder.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes);
	}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		var localizedDialog = new LocalizableMessageDialogViewMap(
				Content: localizer => "[localized]Confirm this message?",
				Title: localizer => "[localized]Confirm?",
				DelayUserInput: true,
				DefaultButtonIndex: 1,
				Buttons: new LocalizableDialogAction[]
				{
								new(LabelProvider: localizer=> localizer!["Y"],Id:"Y"),
								new(LabelProvider: localizer=> localizer!["N"], Id:"N")
				}
			);


		views.Register(
			new ViewMap<ReactiveOnePage, ReactiveOneViewModel>(),
			new DataViewMap<ReactiveTwoPage, ReactiveTwoViewModel, TwoModel>(FromQuery: async (IServiceProvider sp, IDictionary<string, object> args) =>
			{
				if (args.TryGetValue(string.Empty, out var data) &&
					data is ThreeModel threeData)
				{
					return new TwoModel(threeData.Widget with { Name = "Adapted model" });
				}

				return default;
			}),
			new DataViewMap<ReactiveThreePage, ReactiveThreeViewModel, ThreeModel>(),
			new DataViewMap<ReactiveFourPage, ReactiveFourViewModel, FourModel>(),
			new DataViewMap<ReactiveFivePage, ReactiveFiveViewModel, FiveModel>(),
			localizedDialog
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
			Nested: new[]
			{
					new RouteMap("One", View: views.FindByViewModel<ReactiveOneViewModel>()),
					new RouteMap("Two", View: views.FindByViewModel<ReactiveTwoViewModel>()),
					new RouteMap("Three", View: views.FindByViewModel<ReactiveThreeViewModel>(), DependsOn: "Two"),
					new RouteMap("Four", View: views.FindByViewModel<ReactiveFourViewModel>()),
					new RouteMap("Five", View: views.FindByViewModel<ReactiveFiveViewModel>()),
					// Do NOT add "Six" as this has been intentionally omitted from the routemap to test implicit navigation
					new RouteMap("LocalizedConfirm", View: localizedDialog)
			}));
	}
}


