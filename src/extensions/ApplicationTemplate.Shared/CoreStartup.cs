using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using ApplicationTemplate.Presentation;
//using Chinook.BackButtonManager;
//using Chinook.DataLoader;
//using Chinook.DynamicMvvm;
//using Chinook.SectionsNavigation;
//using Chinook.StackNavigation;
// using FluentValidation;
//using MessageDialogService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
//using Nventive.ExtendedSplashScreen;
using Uno.Disposables;
using Windows.UI.Core;

namespace ApplicationTemplate
{
    public sealed class CoreStartup : CoreStartupBase
    {
        protected override void PreInitializeServices()
        {
            LocalizationConfiguration.PreInitialize();
        }

        protected override IHostBuilder InitializeServices(IHostBuilder hostBuilder)
        {
            return hostBuilder;
                //.AddAppLogging()
                //.AddAppSettings()
                //.AddServices();
        }

        protected override void OnInitialized(IServiceProvider services)
        {
            //ViewModelBase.DefaultServiceProvider = services;

            InitializeLoggerFactories(services);

            HandleUnhandledExceptions(services);

            //ValidatorOptions.Global.LanguageManager = new FluentValidationLanguageManager();
        }

        protected override async Task StartServices(IServiceProvider services, bool isFirstStart)
        {
            if (isFirstStart)
            {
                // Start your services here.

                NotifyUserOnSessionExpired(services);

                //services.GetRequiredService<DiagnosticsCountersService>().Start();

                await ExecuteInitialNavigation(CancellationToken.None, services);
            }
        }

        private async Task ExecuteInitialNavigation(CancellationToken ct, IServiceProvider services)
        {
            // TODO: Implement initial navigation
//            var applicationSettingsService = services.GetRequiredService<IApplicationSettingsService>();
//            var sectionsNavigator = services.GetRequiredService<ISectionsNavigator>();

//            var section = await sectionsNavigator.SetActiveSection(ct, "Home");

//            var navigationController = sectionsNavigator.State.ActiveSection;

//            var currentSettings = await applicationSettingsService.GetAndObserveCurrent().FirstAsync(ct);

//            if (currentSettings.IsOnboardingCompleted)
//            {
//                await section.Navigate(ct, () => new HomePageViewModel());
//            }
//            else
//            {
//                await section.Navigate(ct, () => new OnboardingPageViewModel());
//            }
////-:cnd:noEmit
//#if __MOBILE__ || WINDOWS_UWP
////+:cnd:noEmit
//            var dispatcher = services.GetRequiredService<CoreDispatcher>();

//            _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, DismissSplashScreen);

//            void DismissSplashScreen() // Runs on UI thread
//            {
//                Shell.Instance.ExtendedSplashScreen.Dismiss();
//            }
////-:cnd:noEmit
//#endif
////+:cnd:noEmit
        }

        private void NotifyUserOnSessionExpired(IServiceProvider services)
        {
            //var authenticationService = services.GetRequiredService<IAuthenticationService>();
            //var messageDialogService = services.GetRequiredService<IMessageDialogService>();

            //authenticationService
            //    .ObserveSessionExpired()
            //    .SkipWhileSelectMany(async (ct, s) =>
            //    {
            //        await messageDialogService.ShowMessage(ct, mb => mb
            //            .TitleResource("SessionExpired_DialogTitle")
            //            .ContentResource("SessionExpired_DialogBody")
            //            .OkCommand()
            //        );
            //    })
            //    .Subscribe(_ => { }, e => Logger.LogError(e, "Failed to notify user of session expiration."))
            //    .DisposeWith(_neverDisposed);
        }

        private static void InitializeLoggerFactories(IServiceProvider services)
        {
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            //StackNavigationConfiguration.LoggerFactory = loggerFactory;
            //SectionsNavigationConfiguration.LoggerFactory = loggerFactory;
            //BackButtonManagerConfiguration.LoggerFactory = loggerFactory;
            //DynamicMvvmConfiguration.LoggerFactory = loggerFactory;
            //DataLoaderConfiguration.LoggerFactory = loggerFactory;
        }

        private static void HandleUnhandledExceptions(IServiceProvider services)
        {
            //void OnError(Exception e, bool isTerminating = false) => ErrorConfiguration.OnUnhandledException(e, isTerminating, services);

//-:cnd:noEmit
#if WINDOWS_UWP || __ANDROID__ || __IOS__
//+:cnd:noEmit
            //Windows.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
            //{
            //    //OnError(e.Exception);
            //    e.Handled = true;
            //};
//-:cnd:noEmit
#endif
//+:cnd:noEmit
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
            //Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            //{
            //    OnError(e.Exception);
            //    e.Handled = true;
            //};
//-:cnd:noEmit
#endif
//+:cnd:noEmit

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                //OnError(e.Exception);
                e.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exception = e.ExceptionObject as Exception;

                if (exception == null)
                {
                    return;
                }

                #if (IncludeFirebaseAnalytics)
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
                if (exception is Java.Lang.RuntimeException)
                {
                    /// If the exception is a <see cref="Java.Lang.RuntimeException"/> it was already handled and labeled as "Crash" on the console.
                    return;
                }
//-:cnd:noEmit
#endif
//+:cnd:noEmit
#endif

                //OnError(exception, e.IsTerminating);

                #if (IncludeFirebaseAnalytics)
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
                if (e.IsTerminating && Java.Lang.Thread.DefaultUncaughtExceptionHandler != null)
                {
                    /// We need to call <see cref="Java.Lang.Thread.DefaultUncaughtExceptionHandler.UncaughtException"/>
                    /// in order for the crash to be reported by the crash analytics.
                    /// This will re-invoke <see cref="AppDomain.CurrentDomain.UnhandledException"/> with the new exception.
                    Java.Lang.Thread.DefaultUncaughtExceptionHandler.UncaughtException(
                        Java.Lang.Thread.CurrentThread(),
                        new Java.Lang.RuntimeException(Java.Lang.Throwable.FromException(exception))
                    );
                }
//-:cnd:noEmit
#endif
//+:cnd:noEmit
#endif
            };
        }

        protected override ILogger GetOrCreateLogger(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<ILogger<CoreStartup>>();
        }
    }
}
