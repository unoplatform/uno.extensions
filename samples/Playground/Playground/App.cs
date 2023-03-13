using Uno.Extensions;

namespace Playground;

public class App : Application
{
	private static Window? _window;
	public static IHost? Host { get; private set; }

	protected async override void OnLaunched(LaunchActivatedEventArgs args)
	{
#if DEBUG
		if (System.Diagnostics.Debugger.IsAttached)
		{
			// this.DebugSettings.EnableFrameRateCounter = true;
		}
#endif

		var appBuilder = this.CreateBuilder(args)
			.Configure(host => host
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

			.UseHttp((context, services) => services.AddRefitClient<IToDoTaskListEndpoint>(context,
							// Leaving this commented code here as an example of using the settingsBuilder callback
							//,settingsBuilder:
							//		(sp, settings) => settings.AuthorizationHeaderValueGetter =
							//		() => Task.FromResult("AccessToken")
							configure: (builder, endpoint) =>
							builder.ConfigurePrimaryAndInnerHttpMessageHandler<DebugHttpHandler>()

							)
			)
			// Register services for the application
			.ConfigureServices((context, services) =>
			{
				services
					.AddSingleton<IAuthenticationTokenProvider>(new SimpleAuthenticationToken { AccessToken = "My access token" })
					.AddScoped<NeedsADispatcherService>()
					.AddTransient<DebugHttpHandler>()
					.AddContentSerializer(context)
					.AddHostedService<SimpleStartupService>();
			})


			// Enable navigation, including registering views and viewmodels
			.UseNavigation(
				ReactiveViewModelMappings.ViewModelMappings,
				RegisterRoutes,
				configure: cfg => cfg with { AddressBarUpdateEnabled = true })
)
			.UseToolkitNavigation();
		_window = appBuilder.Window;
#if NET5_0_OR_GREATER && WINDOWS
		_window.Activate();
#endif

		var hostingOption = InitOption.Splash;

		switch (hostingOption)
		{
			case InitOption.AdHocHosting:
				// Ad-hoc hosting of Navigation on a UI element with Region.Attached set


				Host = appBuilder.Build();

				// Create Frame and navigate to MainPage
				// MainPage has a ContentControl with Region.Attached set
				// which will host navigation
				var f = new Frame();
				_window.Content = f;
				await _window.AttachServicesAsync(Host.Services);
				f.Navigate(typeof(MainPage));

				await Task.Run(() => Host.StartAsync());

				// With this way there's no way to await for navigation to finish
				// but it's useful if you want to attach navigation to a UI element
				// in an existing application
				break;

			case InitOption.NavigationRoot:
				// Explicitly create the navigation root to use

				Host = appBuilder.Build();

				var root = new ContentControl
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					VerticalContentAlignment = VerticalAlignment.Stretch
				};
				_window.Content = root;
				var services = await _window.AttachServicesAsync(Host.Services);
				var startup = root.HostAsync(services, initialRoute: "");

				await Task.Run(() => Host.StartAsync());

				// Wait for startup task to complete which will be the end of the
				// first navigation
				await startup;
				break;

			case InitOption.InitializeNavigation:
				// InitializeNavigationAsync will create the navigation host (ContentControl),
				// will invoke the host builder (host is returned) and awaits both start up
				// tasks, as well as first navigation

				Host = await _window.InitializeNavigationAsync(async () => appBuilder.Build(),
							// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
							// initialRoute: ""
							// Option 2: Specify route name
							// initialRoute: "Shell"
							// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
							initialViewModel: typeof(ShellViewModel)
						);
				break;

			case InitOption.Splash:
				// InitializeNavigationAsync (Navigation.Toolkit) uses a LoadingView as navigation host,
				// will invoke the host builder (host is returned) and awaits both start up
				// tasks, as well as first navigation. In this case the navigation host is an ExtendedSplashScreen
				// element, so will show the native splash screen until the first navigation is completed

				var appRoot = new AppRoot();
				appRoot.SplashScreen.Initialize(_window, args);

				_window.Content = appRoot;
				_window.Activate();

				Host = await _window.InitializeNavigationAsync(
							async () =>
							{

								// Uncomment to view splashscreen for longer
								// await Task.Delay(5000);
								return appBuilder.Build();
							},
							navigationRoot: appRoot.SplashScreen,
							// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
							// initialRoute: ""
							// Option 2: Specify route name
							// initialRoute: "Shell"
							// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
							initialViewModel: typeof(ShellViewModel)
						);
				break;

			case InitOption.AppBuilderShell:

				Host = await appBuilder.NavigateAsync<AppRoot>();
				break;

			case InitOption.NoShellViewModel:
				// InitializeNavigationAsync with splash screen and async callback to determine where
				// initial navigation should go

				var appRootNoShell = new AppRoot();
				appRootNoShell.SplashScreen.Initialize(_window, args);

				_window.Content = appRootNoShell;
				_window.Activate();

				Host = await _window.InitializeNavigationAsync(
							async () =>
							{
								return appBuilder.Build();
							},
							navigationRoot: appRootNoShell.SplashScreen,
							initialNavigate: async (sp, nav) =>
							{
								// Uncomment to view splashscreen for longer
								await Task.Delay(5000);
								await nav.NavigateViewAsync<HomePage>(this);
							}
						);

				break;
		}

		var notif = Host!.Services.GetRequiredService<IRouteNotifier>();
		notif.RouteChanged += RouteUpdated;


		var logger = Host.Services.GetRequiredService<ILogger<App>>();
		if (logger.IsEnabled(LogLevel.Trace)) logger.LogTraceMessage("LogLevel:Trace");
		if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebugMessage("LogLevel:Debug");
		if (logger.IsEnabled(LogLevel.Information)) logger.LogInformationMessage("LogLevel:Information");
		if (logger.IsEnabled(LogLevel.Warning)) logger.LogWarningMessage("LogLevel:Warning");
		if (logger.IsEnabled(LogLevel.Error)) logger.LogErrorMessage("LogLevel:Error");
		if (logger.IsEnabled(LogLevel.Critical)) logger.LogCriticalMessage("LogLevel:Critical");
	}

	private enum InitOption
	{
		AdHocHosting,
		NavigationRoot,
		InitializeNavigation,
		Splash,
		NoShellViewModel,
		AppBuilderShell
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

	public void RouteUpdated(object? sender, RouteChangedEventArgs? e)
	{
		try
		{
			var rootRegion = e?.Region.Root();
			var route = rootRegion?.GetRoute();
			if (route is null)
			{
				return;
			}


#if !__WASM__ && !WINUI
			CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
			{
				var appTitle = ApplicationView.GetForCurrentView();
				appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
			});
#endif

		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}
}