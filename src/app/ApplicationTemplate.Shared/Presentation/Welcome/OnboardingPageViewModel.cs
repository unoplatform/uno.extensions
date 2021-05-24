using System;
using ApplicationTemplate.Business;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;

namespace ApplicationTemplate.Presentation
{
    public partial class OnboardingPageViewModel : ViewModel
    {
        public IWritableOptions<OnboardingOptions> Onboarding {get;}
        public IRouteMessenger Messenger { get; }
        public OnboardingPageViewModel(
            IWritableOptions<OnboardingOptions> onboarding,
            IRouteMessenger messenger)
        {
            Onboarding = onboarding;
            Messenger = messenger;
        }

        public void CompleteOnboarding()
        {
            Onboarding.Update(options => options.IsOnboardingCompleted = true);
            Messenger.Send<BaseRoutingMessage>(new BaseRoutingMessage(this));
        }

        //public IDynamicCommand NavigateToWelcomePage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IApplicationSettingsService>().CompleteOnboarding(ct);

        //    await this.GetService<IStackNavigator>().NavigateAndClear(ct, () => new WelcomePageViewModel());
        //});

        //public OnboardingItemViewModel[] OnboardingItems { get; } = new[]
        //{
        //    new OnboardingItemViewModel("Page 1", "https://i.imgur.com/hoNgoms.png"),
        //    new OnboardingItemViewModel("Page 2", "https://i.imgur.com/hoNgoms.png"),
        //    new OnboardingItemViewModel("Page 3", "https://i.imgur.com/hoNgoms.png"),
        //};
    }
}
