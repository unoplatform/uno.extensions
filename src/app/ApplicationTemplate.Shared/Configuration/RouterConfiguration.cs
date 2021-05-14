using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using ApplicationTemplate.Presentation;
using ApplicationTemplate.Routing;
using ApplicationTemplate.Views.Content;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApplicationTemplate.Views
{
    /// <summary>
    /// This class is used for navigation configuration.
    /// - Configures the navigator.
    /// </summary>
    public static class RouterConfiguration
    {
        public static IServiceCollection AddRouting(this IServiceCollection services)
        {
            return services
                .AddSingleton<IMessenger, WeakReferenceMessenger>()
                .AddSingleton<IRouteMessenger, RouteMessenger>()
                .AddSingleton<IRouter>(s =>
                new Router(
                    App.Instance.NavigationFrame,
                    s.GetRequiredService<IMessenger>(),
                    //s.GetRequiredService < IDispatcherScheduler>(),
                    GetPageRegistrations(),
                    GetRoutes()
                )
            );
        }

        public static IReadOnlyDictionary<Type, IRoute> GetRoutes() => new Dictionary<Type, IRoute>()
        {
            {typeof(ShowMessage),new Route<ShowMessage>(msg=>{

                return new HomePageViewModel();

            }) }
        };

        public static IReadOnlyDictionary<Type, Type> GetPageRegistrations() => new Dictionary<Type, Type>()
        {
            { typeof(HomePageViewModel), typeof(HomePage) },
            //{ typeof(PostsPageViewModel), typeof(PostsPage) },
            //{ typeof(EditPostPageViewModel), typeof(EditPostPage) },
            //{ typeof(DiagnosticsPageViewModel), typeof(DiagnosticsPage) },
            //{ typeof(WelcomePageViewModel), typeof(WelcomePage) },
            //{ typeof(CreateAccountPageViewModel), typeof(CreateAccountPage) },
            //{ typeof(ForgotPasswordPageViewModel), typeof(ForgotPasswordPage) },
            //{ typeof(LoginPageViewModel), typeof(LoginPage) },
            //{ typeof(OnboardingPageViewModel), typeof(OnboardingPage) },
            //{ typeof(SettingsPageViewModel), typeof(SettingsPage) },
            //{ typeof(LicensesPageViewModel), typeof(LicensesPage) },
            //{ typeof(WebViewPageViewModel), typeof(WebViewPage) },
            //{ typeof(EnvironmentPickerPageViewModel), typeof(EnvironmentPickerPage) },
            //{ typeof(EditProfilePageViewModel), typeof(EditProfilePage) },
            //{ typeof(ChuckNorrisSearchPageViewModel), typeof(ChuckNorrisSearchPage) },
            //{ typeof(ChuckNorrisFavoritesPageViewModel), typeof(ChuckNorrisFavoritesPage) },
        };

    }
}
