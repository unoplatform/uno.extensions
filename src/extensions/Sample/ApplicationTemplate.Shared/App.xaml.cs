using System.Diagnostics;
using System.Threading.Tasks;
using ApplicationTemplate.Views;
//using Chinook.SectionsNavigation;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Display;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.Extensions.Logging.Serilog;
using Uno.Extensions.Logging;
using ApplicationTemplate.Presentation;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Configuration;
using ApplicationTemplate.Business;
using Uno.Extensions.Serialization;
using Uno.Extensions.Localization;
using Uno.Extensions.Navigation.Messages;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Firebase;
using System;
using Windows.Storage;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Text.Json;

//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.Foundation;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate
{

  
sealed partial class App : Application
    {
        private IHost host { get; }
        public App()
        {
            Instance = this;

            //var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            //Console.WriteLine($"Local '{ localFolder.Path}'");
            //var methods = localFolder.GetType().GetMethods(
            //    System.Reflection.BindingFlags.Instance |
            //    System.Reflection.BindingFlags.NonPublic |
            //    System.Reflection.BindingFlags.Public |
            //    System.Reflection.BindingFlags.FlattenHierarchy |
            //    System.Reflection.BindingFlags.IgnoreCase);
            //foreach (var m in methods)
            //{
            //    Console.WriteLine($"Method '{m.Name}'");
            //}
            //var implmethod= localFolder.GetType().GetMethod("get_Implementation",
            //    System.Reflection.BindingFlags.Instance |
            //    System.Reflection.BindingFlags.NonPublic |
            //    System.Reflection.BindingFlags.Public |
            //    System.Reflection.BindingFlags.FlattenHierarchy |
            //    System.Reflection.BindingFlags.IgnoreCase);

            //var impl = implmethod.Invoke(localFolder, null);
            //methods = impl.GetType().GetMethods(
            //    System.Reflection.BindingFlags.Instance |
            //    System.Reflection.BindingFlags.NonPublic |
            //    System.Reflection.BindingFlags.Public |
            //    System.Reflection.BindingFlags.FlattenHierarchy |
            //    System.Reflection.BindingFlags.IgnoreCase);
            //foreach (var m in methods)
            //{
            //    Console.WriteLine($"Imp - Method '{m.Name}'");
            //}

            ////method.Invoke(localFolder,null);
            //Console.WriteLine($"Local created '{ localFolder.Path}'");
            //var configTask = localFolder.CreateFolderAsync("config", CreationCollisionOption.OpenIfExists);


            host = UnoHost
                .CreateDefaultBuilder()
                .UseEnvironment("Staging")
                .UseAppSettingsForHostConfiguration<App>()
                .UseHostConfigurationForApp()
                .UseEnvironmentAppSettings<App>()
                .UseLocalization()
                //.UseWritableSettings<EndpointOptions>(ctx => ctx.Configuration.GetSection("ChuckNorrisEndpoint"))
                .UseWritableSettings<AuthenticationData>(ctx => ctx.Configuration.GetSection(nameof(AuthenticationData)))
                .UseWritableSettings<ApplicationSettings>(ctx => ctx.Configuration.GetSection(nameof(ApplicationSettings)))
                .UseWritableSettings<DiagnosticSettings>(ctx => ctx.Configuration.GetSection(nameof(DiagnosticSettings)))
                //.UseRouting<RouterConfiguration, LaunchMessage>(() => App.Instance.NavigationFrame)
                .UseRoutingWithRedirection<RouterConfiguration, LaunchMessage, RouterRedirection>(() => App.Instance.NavigationFrame)
                .AddApi()
                .UseFirebaseHandler()
                .UseSerialization()
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        //.AddSerialization(SerializationGeneratorConfiguration.Initialize)
                        //.AddSystemTextJsonSerialization()
                        .AddAppServices()
                        .AddTransient<ShellViewModel>()
                        .AddTransient<DiagnosticsOverlayViewModel>()
                        .AddTransient<CreateAccountFormViewModel>()
                        .AddTransient<LoginFormViewModel>()
                        .AddTransient<ForgotPasswordFormViewModel>()
                        .AddTransient<EditProfileFormViewModel>()
                        .AddTransient<MenuViewModel>();
                })
                .UseUnoLogging(logBuilder =>
                {
                    logBuilder
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)
                    .CoreLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .XamlBindingLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .XamlLayoutLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .XamlLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .StorageLogLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                    .XamlBindingLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .BinderMemoryReferenceLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .HotReloadCoreLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical)
                    .WebAssemblyLogLevel(Microsoft.Extensions.Logging.LogLevel.Critical);
                }
#if __WASM__
                    , new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider()
#endif
                )
                .UseSerilog(true)
                .Build()
                .EnableUnoLogging();

            Ioc.Default.ConfigureServices(host.Services);
            //host = UnoHost.CreateDefaultHostWithStartup<AppServiceConfigurer>();
            //var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            //var services = host.Services;
            //lifetime.ApplicationStarted.Register(() =>
            //{
            //    //var router = services.GetRequiredService<IRouter>();
            //    //var messenger = services.GetRequiredService<IMessenger>();
            //    //messenger.Send<BaseRoutingMessage>(new ShowMessage(this));

            //});
            //lifetime.ApplicationStopping.Register(() =>
            //{
            //});
            //lifetime.ApplicationStopped.Register(() =>
            //{
            //});

            //Startup = new Startup();
            //Startup.PreInitialize();

            InitializeComponent();

            ConfigureOrientation();
        }

        public Activity ShellActivity { get; } = new Activity(nameof(Shell));

        public static App Instance { get; private set; }

        public static Startup Startup { get; private set; }

        public Shell Shell { get; private set; }

        public Frame NavigationFrame => Shell?.NavigationFrame;

        public Window CurrentWindow { get; private set; }

        //-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
        protected override void OnLaunched(Windows.ApplicationModel.Activation.LaunchActivatedEventArgs args)
        {
            InitializeAndStart(args);
        }
//-:cnd:noEmit
#else
        //+:cnd:noEmit
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            InitializeAndStart(args.UWPLaunchActivatedEventArgs);
        }
        //-:cnd:noEmit
#endif
        //+:cnd:noEmit

        //-:cnd:noEmit
#if !(NET5_0 && WINDOWS)
        //+:cnd:noEmit
        //protected override void OnActivated(IActivatedEventArgs args)
        //{
        //    // This is where your app launches if you use custom schemes, Universal Links, or Android App Links.
        //    InitializeAndStart(args);
        //}
        //-:cnd:noEmit
#endif
        //+:cnd:noEmit

        private void InitializeAndStart(IActivatedEventArgs args)
        {
#if NET5_0 && WINDOWS
            CurrentWindow = new Window();
#elif !WINUI
			CurrentWindow = Window.Current;
#else
            CurrentWindow = Microsoft.UI.Xaml.Window.Current;
#endif

            Shell = CurrentWindow.Content as Shell;

            var isFirstLaunch = Shell == null;

            if (isFirstLaunch)
            {
                ConfigureViewSize();
                ConfigureStatusBar();

                //Startup.Initialize();

                //#if (IncludeFirebaseAnalytics)
                //                ConfigureFirebase();
                //#endif

                ShellActivity.Start();

                CurrentWindow.Content = Shell = new Shell(args);
                Shell.DataContext = host.Services.GetService<ShellViewModel>();

                ShellActivity.Stop();

            }

            CurrentWindow.Activate();

            _ = Task.Run(() =>
            {
                //Startup.Start();
                host.Run();
            });
        }

        private void ConfigureOrientation()
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        private void ConfigureViewSize()
        {
            //-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 480));
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
        }

        private void ConfigureStatusBar()
        {
            var resources = Application.Current.Resources;

            //-:cnd:noEmit
#if !WINUI || (NET5_0 && WINDOWS) || HAS_UNO_SKIA || __WASM__
            //+:cnd:noEmit
            var statusBarHeight = 0;
            //-:cnd:noEmit
#else
            //+:cnd:noEmit
            var statusBarHeight = Windows.UI.ViewManagement.StatusBar.GetForCurrentView().OccludedRect.Height;
                        Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ForegroundColor = Windows.UI.Colors.White;
            //-:cnd:noEmit
#endif
            //+:cnd:noEmit

            resources.Add("StatusBarDouble", (double)statusBarHeight);
            resources.Add("StatusBarThickness", new Thickness(0, statusBarHeight, 0, 0));
            resources.Add("StatusBarGridLength", new GridLength(statusBarHeight, GridUnitType.Pixel));
        }

        //#if (IncludeFirebaseAnalytics)
        //        private void ConfigureFirebase()
        //        {
        ////-:cnd:noEmit
        //#if __IOS__
        ////+:cnd:noEmit
        //            // This is used to initalize firebase and crashlytics.
        //            Firebase.Core.App.Configure();
        ////-:cnd:noEmit
        //#endif
        ////+:cnd:noEmit
        //        }
        //#endif
    }
}
