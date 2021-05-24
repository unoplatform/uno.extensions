using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using Uno.Extensions.Navigation;
using ApplicationTemplate.Presentation;
using ApplicationTemplate.Views.Content;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Specialized;
using Microsoft.Extensions.Options;
using Uno.Extensions.Configuration;

namespace ApplicationTemplate.Views
{
    /// <summary>
    /// This class is used for navigation configuration.
    /// - Configures the navigator.
    /// </summary>
    public class RouterConfiguration : IRouteDefinitions
    {
        //public static IServiceCollection AddRouting(this IServiceCollection services)
        //{
        //    return services
        //        .AddSingleton<IMessenger, WeakReferenceMessenger>()
        //        .AddSingleton<IRouteMessenger, RouteMessenger>()
        //        .AddSingleton<IRouter>(s =>
        //        new Router(
        //            App.Instance.NavigationFrame,
        //            s.GetRequiredService<IMessenger>(),
        //            //s.GetRequiredService < IDispatcherScheduler>(),
        //            GetPageRegistrations(),
        //            GetRoutes()
        //        )
        //    );
        //}

        //public IReadOnlyDictionary<Type, IRoute> Routes { get; }
        //= new Dictionary<Type, IRoute>()
        //{
        //    {typeof(ShowMessage),new Route<ShowMessage>(msg=>{

        //        return Ioc.Default.GetService<HomePageViewModel>();

        //    }) },
        //    {typeof(LaunchMessage),new Route<LaunchMessage>(msg=>{

        //        return Ioc.Default.GetService<HomePageViewModel>();

        //    }) }
        //};

        public IReadOnlyDictionary<string, (Type,Action<IServiceCollection>, Func<IServiceProvider, object>)> Routes { get; } = new Dictionary<string, (Type, Action<IServiceCollection>, Func<IServiceProvider, object>)>()
        {
            {typeof(HomePageViewModel).Name.ToLower(), (typeof(HomePage),services=>services.AddTransient<HomePageViewModel>(),sp=>sp.GetService<HomePageViewModel>() ) },
            {typeof(OnboardingPageViewModel).Name.ToLower(), (typeof(OnboardingPage),services=>services.AddTransient<OnboardingPageViewModel>(),sp=>sp.GetService<OnboardingPageViewModel>() ) },
            {typeof(WelcomePageViewModel).Name.ToLower(), (typeof(WelcomePage),services=>services.AddTransient<WelcomePageViewModel>(),sp=>sp.GetService<WelcomePageViewModel>() ) }
        };

        public IReadOnlyDictionary<string, Func<IServiceProvider, string[], string, string>> Redirections { get; } = new Dictionary<string, Func<IServiceProvider, string[], string, string>>()
        {
            {"",(sp, stack,route)=>{
                var onboarding = sp.GetService<IWritableOptions<OnboardingOptions>>();
                if(!onboarding.Value.IsOnboardingCompleted){
                    return typeof(OnboardingPageViewModel).Name.ToLower();
                }
                else{
                    return typeof(WelcomePageViewModel).Name.ToLower();
                    }
            } }
        };


        //public IReadOnlyDictionary<Type, Type> ViewModelMappings { get;} = new Dictionary<Type, Type>()
        //{
        //    { typeof(HomePageViewModel), typeof(HomePage) },
        //    //{ typeof(PostsPageViewModel), typeof(PostsPage) },
        //    //{ typeof(EditPostPageViewModel), typeof(EditPostPage) },
        //    //{ typeof(DiagnosticsPageViewModel), typeof(DiagnosticsPage) },
        //    //{ typeof(WelcomePageViewModel), typeof(WelcomePage) },
        //    //{ typeof(CreateAccountPageViewModel), typeof(CreateAccountPage) },
        //    //{ typeof(ForgotPasswordPageViewModel), typeof(ForgotPasswordPage) },
        //    //{ typeof(LoginPageViewModel), typeof(LoginPage) },
        //    //{ typeof(OnboardingPageViewModel), typeof(OnboardingPage) },
        //    //{ typeof(SettingsPageViewModel), typeof(SettingsPage) },
        //    //{ typeof(LicensesPageViewModel), typeof(LicensesPage) },
        //    //{ typeof(WebViewPageViewModel), typeof(WebViewPage) },
        //    //{ typeof(EnvironmentPickerPageViewModel), typeof(EnvironmentPickerPage) },
        //    //{ typeof(EditProfilePageViewModel), typeof(EditProfilePage) },
        //    //{ typeof(ChuckNorrisSearchPageViewModel), typeof(ChuckNorrisSearchPage) },
        //    //{ typeof(ChuckNorrisFavoritesPageViewModel), typeof(ChuckNorrisFavoritesPage) },
        //};

    }
}
