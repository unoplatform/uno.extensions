using Uno.Extensions.Reactive;

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
			new ViewMap<ReactiveThreePage, ReactiveThreeViewModel>(Data: new ReactiveDataMap<ThreeModel>(
				FromQuery: async (sp, dict) =>
				{
					var model = dict[""] as ThreeModel;
					if(model is null)
					{
						var name = dict[""] as string;
						model = new ThreeModel(new ReactiveWidget(name??"Invalid", 50.0));
					}
					return Feed<ThreeModel>.Async(async ct => {
						await Task.Delay(2000);
						return model!; });
				})),
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


public record ReactiveDataMap<TData>(
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<IFeed<TData>>>? FromQuery = null
) : DataMap(
	typeof(TData),
	(object data) => (ToQuery is not null && data is TData tdata) ? ToQuery(tdata) : new Dictionary<string, string>(),
	async (IServiceProvider sp, IDictionary<string, object> query) => await ((FromQuery is not null && query is not null) ? FromQuery(sp, query) : Task.FromResult<IFeed<TData>>(default!)))
	where TData : class
{
	public override void RegisterTypes(IServiceCollection services)
	{
		services.AddViewModelData<IFeed<TData>>();
	}
}

