using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationTemplate.Business;
using CommunityToolkit.Mvvm.Input;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;

namespace ApplicationTemplate.Presentation
{
    public partial class OnboardingPageViewModel : ViewModel
    {
        public IWritableOptions<ApplicationSettings> Onboarding {get;}
        public IRouteMessenger Messenger { get; }
        public OnboardingPageViewModel(
            IWritableOptions<ApplicationSettings> onboarding,
            IRouteMessenger messenger)
        {
            Onboarding = onboarding;
            Messenger = messenger;
        }

        private async void CompleteOnboarding()
        {
            await Onboarding.Update(options => options.IsOnboardingCompleted = true);
            Messenger.Send(new ClearStackMessage(this));
        }

        //public IDynamicCommand NavigateToWelcomePage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IApplicationSettingsService>().CompleteOnboarding(ct);

        //    await this.GetService<IStackNavigator>().NavigateAndClear(ct, () => new WelcomePageViewModel());
        //});

        public ICommand NavigateToWelcomePage => new RelayCommand(CompleteOnboarding);

        public OnboardingItemViewModel[] OnboardingItems { get; } = new[]
        {
            new OnboardingItemViewModel("Page 1", "https://i.imgur.com/hoNgoms.png"),
            new OnboardingItemViewModel("Page 2", "https://i.imgur.com/hoNgoms.png"),
            new OnboardingItemViewModel("Page 3", "https://i.imgur.com/hoNgoms.png"),
        };
    }
}
