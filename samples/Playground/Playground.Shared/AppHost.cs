namespace Playground;
internal static class AppHost
{
	public static IApplicationBuilder ConfigureApp(this IApplicationBuilder builder)
	{
		return builder.Configure(host => host
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
			}, enableUnoLogging: true)

			.UseLocalization()

			.UseConfiguration(configure: configBuilder =>
				configBuilder
					.EmbeddedSource<App>()          // appsettings.json + appsettings.development.json
					.EmbeddedSource<App>("platform")    // appsettings.platform.json
					.Section<Playground.Models.AppInfo>()
			)

			.UseThemeSwitching()

			// Register Json serializer jsontypeinfo definitions
			.UseSerialization(
				services => services
					.AddJsonTypeInfo(WidgetContext.Default.Widget)
					.AddJsonTypeInfo(PersonContext.Default.Person)
			)

			// Register services for the application
			.ConfigureServices((context, services) =>
			{
				services
					.AddSingleton<IAuthenticationTokenProvider>(new SimpleAuthenticationToken { AccessToken = "My access token" })
					.AddScoped<NeedsADispatcherService>()
					.AddNativeHandler(context)
					.AddTransient<DebugHttpHandler>()
					.AddContentSerializer(context)

					.AddRefitClient<IToDoTaskListEndpoint>(context,
							// Leaving this commented code here as an example of using the settingsBuilder callback
							//,settingsBuilder:
							//		(sp, settings) => settings.AuthorizationHeaderValueGetter =
							//		() => Task.FromResult("AccessToken")
							configure: (builder, endpoint) =>
							builder.ConfigurePrimaryAndInnerHttpMessageHandler<DebugHttpHandler>()

							)

					.AddHostedService<SimpleStartupService>()
					.AddHostedService<LongStartHostedService>();
			})


			// Enable navigation, including registering views and viewmodels
			.UseNavigation(
				PlaygroundApp.ReactiveViewModelMappings.ViewModelMappings,
				RegisterRoutes,
				configure: cfg => cfg with { AddressBarUpdateEnabled = true }));
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
			// Option 1: Specify ShellView in order to customise the shell
			//new ViewMap<ShellView, ShellViewModel>(),
			// Option 2: Only specify the ShellViewModel - this will inject a FrameView where the subsequent pages will be shown
			new ViewMap(ViewModel: typeof(ShellViewModel)),
			new ViewMap<HomePage, HomeViewModel>(),
			new ViewMap<CodeBehindPage>(),
			new ViewMap<VMPage, VMViewModel>(),
			new ViewMap<XamlPage, XamlViewModel>(),
			new ViewMap<NavigationViewPage, NavigationViewViewModel>(),
			new ViewMap<NavContentPage, NavContentViewModel>(Data: new DataMap<NavWidget>()),
			new ViewMap<NavContentSecondPage>(),
			new ViewMap<TabBarPage>(),
			new ViewMap<ContentControlPage>(),
			new ViewMap<SecondPage, SecondViewModel>(Data: new DataMap<Widget>(), ResultData: typeof(Country)),
			new ViewMap<ThirdPage>(),
			new ViewMap<FourthPage, FourthViewModel>(),
			new ViewMap<FifthPage, FifthViewModel>(),
			new ViewMap<DialogsPage>(),
			new ViewMap<SimpleDialog, SimpleViewModel>(),
			new ViewMap<ComplexDialog>(),
			new ViewMap<ComplexDialogFirstPage>(),
			new ViewMap<ComplexDialogSecondPage>(),
			new ViewMap<PanelVisibilityPage>(),
			new ViewMap<VisualStatesPage>(),
			new ViewMap<AdHocPage, AdHocViewModel>(),
			new ViewMap<ListPage, ListViewModel>(),
			new ViewMap<ItemDetailsPage, ItemDetailsViewModel>(),
			new ViewMap<AuthTokenDialog, AuthTokenViewModel>(),
			new ViewMap<BasicFlyout, BasicViewModel>(),
			new ViewMap<ThemeSwitchPage, ThemeSwitchViewModel>(),
			confirmDialog,
			localizedDialog
		);


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
			Nested: new[]
			{
					new RouteMap("Home",View: views.FindByView<HomePage>()),
					new RouteMap("CodeBehind",View: views.FindByView<CodeBehindPage>(), DependsOn: "Home"),
					new RouteMap("VM",View: views.FindByView<VMPage>(), DependsOn: "Home"),
					new RouteMap("Xaml",View: views.FindByView<XamlPage>(), DependsOn: "Home"),
					new RouteMap("NavigationView",View: views.FindByView<NavigationViewPage>(), DependsOn: "Home",
					Nested: new[]
					{
						new RouteMap("NavContent", View: views.FindByViewModel<NavContentViewModel>(),
						Nested: new[]
						{
							new RouteMap("NavContentTabs", IsDefault:true,
										Nested: new[]
										{
											new RouteMap("Tab1", IsDefault:true),
											new RouteMap("Tab2")
										}),
						}),
						new RouteMap("NavContentSecond", View: views.FindByView<NavContentSecondPage>(), DependsOn:"NavContent")
					}),
					new RouteMap("TabBar",View: views.FindByView<TabBarPage>(), DependsOn: "Home"),
					new RouteMap("ContentControl",View: views.FindByView<ContentControlPage>(), DependsOn: "Home"),
					new RouteMap("Second",View: views.FindByView<SecondPage>(), DependsOn: "Home"),
					new RouteMap("Third",View: views.FindByView<ThirdPage>()),
					new RouteMap("Fourth",View: views.FindByView<FourthPage>()),
					new RouteMap("Fifth",View: views.FindByView<FifthPage>(), DependsOn: "Third"),
					new RouteMap("Dialogs",View: views.FindByView<DialogsPage>(),
					Nested: new[]
					{
						new RouteMap("Simple",View: views.FindByView<SimpleDialog>()),
						new RouteMap("Complex",View: views.FindByView<ComplexDialog>(),
						Nested: new[]
						{
							new RouteMap("ComplexDialogFirst",View: views.FindByView<ComplexDialogFirstPage>()),
							new RouteMap("ComplexDialogSecond",View: views.FindByView<ComplexDialogSecondPage>(), DependsOn: "ComplexDialogFirst")
						})
					}),
					new RouteMap("PanelVisibility",View: views.FindByView<PanelVisibilityPage>()),
					new RouteMap("VisualStates",View: views.FindByView<VisualStatesPage>()),
					new RouteMap("AdHoc",View: views.FindByViewModel<AdHocViewModel>(),
					Nested: new[]
					{
						new RouteMap("Auth", View: views.FindByView<AuthTokenDialog>())
					}),
					new RouteMap("List",View: views.FindByViewModel<ListViewModel>()),
					new RouteMap("ItemDetails",View: views.FindByViewModel<ItemDetailsViewModel>()),
					new RouteMap("Confirm", View: confirmDialog),
					new RouteMap("LocalizedConfirm", View: localizedDialog)
			}));
	}
}
