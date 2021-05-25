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

        public IReadOnlyDictionary<string, (Type, Action<IServiceCollection>, Func<IServiceProvider, object>)> Routes { get; } = new Dictionary<string, (Type, Action<IServiceCollection>, Func<IServiceProvider, object>)>()
        {
            {typeof(HomePageViewModel).Name.ToLower(), (typeof(HomePage),services=>services.AddTransient<HomePageViewModel>(),sp=>sp.GetService<HomePageViewModel>() ) },
            {typeof(OnboardingPageViewModel).Name.ToLower(), (typeof(OnboardingPage),services=>services.AddTransient<OnboardingPageViewModel>(),sp=>sp.GetService<OnboardingPageViewModel>() ) },
            {typeof(WelcomePageViewModel).Name.ToLower(), (typeof(WelcomePage),services=>services.AddTransient<WelcomePageViewModel>(),sp=>sp.GetService<WelcomePageViewModel>() ) },
            {typeof(LoginPageViewModel).Name.ToLower(), (typeof(LoginPage),services=>services.AddTransient<LoginPageViewModel>(),sp=>sp.GetService<LoginPageViewModel>() ) },
            {typeof(CreateAccountPageViewModel).Name.ToLower(), (typeof(CreateAccountPage),services=>services.AddTransient<CreateAccountPageViewModel>(),sp=>sp.GetService<CreateAccountPageViewModel>() ) },
            {typeof(ForgotPasswordPageViewModel).Name.ToLower(), (typeof(ForgotPasswordPage),services=>services.AddTransient<ForgotPasswordPageViewModel>(),sp=>sp.GetService<ForgotPasswordPageViewModel>() ) },
            {typeof(PostsPageViewModel).Name.ToLower(), (typeof(PostsPage),services=>services.AddTransient<PostsPageViewModel>(),sp=>sp.GetService<PostsPageViewModel>() ) },
            {typeof(ChuckNorrisSearchPageViewModel).Name.ToLower(), (typeof(ChuckNorrisSearchPage),services=>services.AddTransient<ChuckNorrisSearchPageViewModel>(),sp=>sp.GetService<ChuckNorrisSearchPageViewModel>() ) },
            {typeof(SettingsPageViewModel).Name.ToLower(), (typeof(SettingsPage),services=>services.AddTransient<SettingsPageViewModel>(),sp=>sp.GetService<SettingsPageViewModel>() ) },
            {typeof(DiagnosticsPageViewModel).Name.ToLower(), (typeof(DiagnosticsPage),services=>services.AddTransient<DiagnosticsPageViewModel>(),sp=>sp.GetService<DiagnosticsPageViewModel>() ) },
            {typeof(LicensesPageViewModel).Name.ToLower(), (typeof(LicensesPage),services=>services.AddTransient<LicensesPageViewModel>(),sp=>sp.GetService<LicensesPageViewModel>() ) },
            {typeof(EditProfilePageViewModel).Name.ToLower(), (typeof(EditProfilePage),services=>services.AddTransient<EditProfilePageViewModel>(),sp=>sp.GetService<EditProfilePageViewModel>() ) }
        };

        public IReadOnlyDictionary<
            string,
            Func<
                IServiceProvider,               // Service provider for looking up services
                string[],                       // navigation stack
                string,                         // new path
                IDictionary<string, object>,    // args
                string                          // relative path
                >> Redirections
        { get; } = new Dictionary<string, Func<IServiceProvider, string[], string, IDictionary<string, object>, string>>()
        {
            {"",(sp, stack,route, args)=>{
                    if(args is not null && args.TryGetValue(RouterConfiguration.ActionsKey, out var action)) {
                        if(((RouterConfiguration.Actions)action) == RouterConfiguration.Actions.Login)
                        {
                            return typeof(LoginPageViewModel).Name.ToLower();
                        }
                    }

                    var onboarding = sp.GetService<IWritableOptions<ApplicationSettings>>();
                    if(!onboarding.Value.IsOnboardingCompleted){
                        return typeof(OnboardingPageViewModel).Name.ToLower();
                    }
                    else{
                        return typeof(WelcomePageViewModel).Name.ToLower();
                    }
                }
            }
        };


    }
}
