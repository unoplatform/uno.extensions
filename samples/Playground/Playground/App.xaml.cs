namespace Playground;
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();
#if DEBUG
        MainWindow.EnableHotReload();
#endif

        var appBuilder = this.CreateBuilder(args)
                    .ConfigureApp()
                    .UseToolkitNavigation();
        MainWindow = appBuilder.Window;

        var hostingOption = InitOption.Splash;

        switch (hostingOption)
        {
            case InitOption.AdHocHosting:
                // Ad-hoc hosting of Navigation on a UI element with Region.Attached set


                _host = appBuilder.Build();

                // Create Frame and navigate to MainPage
                // MainPage has a ContentControl with Region.Attached set
                // which will host navigation
                var f = new Frame();
                MainWindow.Content = f;
                await MainWindow.AttachServicesAsync(_host.Services);
                f.Navigate(typeof(MainPage));

                await Task.Run(() => _host.StartAsync());

                // With this way there's no way to await for navigation to finish
                // but it's useful if you want to attach navigation to a UI element
                // in an existing application
                break;

            case InitOption.NavigationRoot:
                // Explicitly create the navigation root to use

                _host = appBuilder.Build();

                var root = new ContentControl
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };
                MainWindow.Content = root;
                var services = await MainWindow.AttachServicesAsync(_host.Services);
                var startup = root.HostAsync(services, initialRoute: "");

                await Task.Run(() => _host.StartAsync());

                // Wait for startup task to complete which will be the end of the
                // first navigation
                await startup;
                break;

            case InitOption.InitializeNavigation:
                // InitializeNavigationAsync will create the navigation host (ContentControl),
                // will invoke the host builder (host is returned) and awaits both start up
                // tasks, as well as first navigation

                _host = await MainWindow.InitializeNavigationAsync(async () => appBuilder.Build(),
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
                appRoot.SplashScreen.Initialize(MainWindow, args);
                MainWindow.Content = appRoot;

                _host = await MainWindow.InitializeNavigationAsync(
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
                            initialViewModel: typeof(HomeViewModel)
                        );
                break;
            case InitOption.AppBuilderShell:

                _host = await appBuilder.NavigateAsync<AppRoot>();
                break;

            case InitOption.NoShellViewModel:
                // InitializeNavigationAsync with splash screen and async callback to determine where
                // initial navigation should go

                var appRootNoShell = new AppRoot();
                appRootNoShell.SplashScreen.Initialize(MainWindow, args);

                MainWindow.Content = appRootNoShell;
                MainWindow.Activate();

                _host = await MainWindow.InitializeNavigationAsync(
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

        var notif = _host!.Services.GetRequiredService<IRouteNotifier>();
        notif.RouteChanged += RouteUpdated;


        var logger = _host.Services.GetRequiredService<ILogger<App>>();
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

public class LongStartHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.Delay(2000, cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
