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
			new DataViewMap<ReactiveTwoPage, ReactiveTwoViewModel, TwoModel>(),
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
					new RouteMap("Three", View: views.FindByViewModel<ReactiveThreeViewModel>()),
					new RouteMap("Four", View: views.FindByViewModel<ReactiveFourViewModel>()),
					new RouteMap("Five", View: views.FindByViewModel<ReactiveFiveViewModel>()),
					new RouteMap("LocalizedConfirm", View: localizedDialog)
			}));
	}
}


