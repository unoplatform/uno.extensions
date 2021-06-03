using System;
using System.Collections.Generic;
using ApplicationTemplate.Business;
using ApplicationTemplate.Presentation;
using ApplicationTemplate.Views.Content;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;

namespace ApplicationTemplate.Views
{
    /// <summary>
    /// This class is used for navigation configuration.
    /// - Configures the navigator.
    /// </summary>
    public class RouterConfiguration : IRouteDefinitions
    {
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
