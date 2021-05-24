using System;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class WelcomePageViewModel : ViewModel
    {
        public IWritableOptions<OnboardingOptions> Onboarding { get; }
        public IRouteMessenger Messenger { get; }
        public WelcomePageViewModel(
            IWritableOptions<OnboardingOptions> onboarding,
            IRouteMessenger messenger)
        {
            Onboarding = onboarding;
            Messenger = messenger;
        }

        public void ResetOnboarding()
        {
            Onboarding.Update(options => options.IsOnboardingCompleted = false);
            Messenger.Send<BaseRoutingMessage>(new BaseRoutingMessage(this));
        }
        //public IDynamicCommand NavigateToHomePage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new HomePageViewModel());
        //});

        //public IDynamicCommand NavigateToLoginPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new LoginPageViewModel(async ct2 =>
        //    {
        //        await this.GetService<IStackNavigator>().NavigateAndClear(ct2, () => new HomePageViewModel());
        //    }));
        //});

        //public IDynamicCommand NavigateToCreateAccountPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new CreateAccountPageViewModel());
        //});
    }
}
