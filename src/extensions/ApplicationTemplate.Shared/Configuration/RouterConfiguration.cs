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
using ApplicationTemplate.Business;

namespace ApplicationTemplate.Views
{
    public static class RouteTypeExtensions
    {
        public static string AsRoute(this Type routeViewModel)
        {
            return routeViewModel.Name.ToLower().Replace("pageviewmodel", "");
        }

        public static Dictionary<string,(Type,Type)> RegisterPage<TViewModel,TPage>(this Dictionary<string, (Type, Type)> routeDictionary)
        {
            routeDictionary[typeof(TViewModel).AsRoute()] = (typeof(TPage), typeof(TViewModel));
            return routeDictionary;
        }
    }

    /// <summary>
    /// This class is used for navigation configuration.
    /// - Configures the navigator.
    /// </summary>
    public class RouterConfiguration : IRouteDefinitions
    {
        public const string ActionsKey = "action";

        public enum Actions
        {
            Login
        }


        public IReadOnlyDictionary<string, (Type, Type)> Routes { get; } = new Dictionary<string, (Type, Type)>()
                        .RegisterPage<HomePageViewModel, HomePage>()
                        .RegisterPage<OnboardingPageViewModel, OnboardingPage>()
                        .RegisterPage<WelcomePageViewModel, WelcomePage>()
                        .RegisterPage<LoginPageViewModel, LoginPage>()
                        .RegisterPage<CreateAccountPageViewModel, CreateAccountPage>()
                        .RegisterPage<ForgotPasswordPageViewModel, ForgotPasswordPage>()
                        .RegisterPage<PostsPageViewModel, PostsPage>()
                        .RegisterPage<ChuckNorrisSearchPageViewModel, ChuckNorrisSearchPage>()
                        .RegisterPage<SettingsPageViewModel, SettingsPage>()
                        .RegisterPage<DiagnosticsPageViewModel, DiagnosticsPage>()
                        .RegisterPage<LicensesPageViewModel, LicensesPage>()
                        .RegisterPage<EditProfilePageViewModel, EditProfilePage>();

    }


    public class RouterRedirection : IRouteRedirection
    {
        private IServiceProvider Services { get; }
        public RouterRedirection(IServiceProvider services)
        {
            Services = services;
            Redirection =
                (stack, route, args) => {
                    if (args is not null && args.TryGetValue(RouterConfiguration.ActionsKey, out var action))
                    {
                        if (((RouterConfiguration.Actions)action) == RouterConfiguration.Actions.Login)
                        {
                            return typeof(LoginPageViewModel).AsRoute();
                        }
                    }

                    if (route != "") return route;
                    var onboarding = Services.GetService<IWritableOptions<ApplicationSettings>>();
                    if (!onboarding.Value.IsOnboardingCompleted)
                    {
                        return typeof(OnboardingPageViewModel).AsRoute();
                    }
                    else
                    {
                        return typeof(WelcomePageViewModel).AsRoute();
                    }
                };
        }

    
        public Func<
                string[],                       // navigation stack
                string,                         // new path
                IDictionary<string, object>,    // args
                string                          // relative path
                > Redirection
        { get; }


    }
}
