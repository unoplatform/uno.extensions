

namespace TestHarness.Ext.Navigation.Dialogs;

public class DialogsHostInit : BaseHostInitialization
{
	protected override IHostBuilder Navigation(IHostBuilder builder)
	{
		return builder.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes);
	}

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
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
			new ViewMap<DialogsFlyoutsPage, DialogsFlyoutsViewModel>(),
			new ViewMap<DialogsComplexDialog>(),
			new ViewMap<DialogsComplexDialogFirstPage, DialogsComplexDialogFirstViewModel>(),
			new ViewMap<DialogsComplexDialogSecondPage, DialogsComplexDialogSecondViewModel>(),
			new ViewMap<DialogsComplexFlyout>( ResultData: typeof(DialogsFlyoutsData)),
			new ViewMap<DialogsComplexFlyoutOnePage, DialogsComplexFlyoutOneViewModel>(),
			new ViewMap<DialogsComplexFlyoutTwoPage, DialogsComplexFlyoutTwoViewModel>(),
			confirmDialog,
			localizedDialog
			);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("",
			Nested: new[]
			{
				new RouteMap("DialogsFlyouts", View: views.FindByViewModel<DialogsFlyoutsViewModel>()),
				new RouteMap("DialogsComplex", View: views.FindByView<DialogsComplexDialog>(), Nested: new[]
				{
					new RouteMap("DialogsComplexDialogFirst", View: views.FindByViewModel<DialogsComplexDialogFirstViewModel>(), IsDefault:true),
					new RouteMap("DialogsComplexDialogSecond", View: views.FindByViewModel<DialogsComplexDialogSecondViewModel>())
				}),
				new RouteMap("DialogsComplexFlyout", View: views.FindByView<DialogsComplexFlyout>(), Nested: new[]
				{
					new RouteMap("DialogsComplexFlyoutFirst", View: views.FindByViewModel<DialogsComplexFlyoutOneViewModel>(), IsDefault:true),
					new RouteMap("DialogsComplexFlyoutSecond", View: views.FindByViewModel<DialogsComplexFlyoutTwoViewModel>(), DependsOn: "DialogsComplexFlyoutFirst")
				}),
				new RouteMap("Confirm", View: confirmDialog),
				new RouteMap("LocalizedConfirm", View: localizedDialog)
			}));
	}

}
