using System;
using ApplicationTemplate.Business;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class OnboardingPageViewModel : ViewModel
    {
        public IDynamicCommand NavigateToWelcomePage => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IApplicationSettingsService>().CompleteOnboarding(ct);

            await this.GetService<IStackNavigator>().NavigateAndClear(ct, () => new WelcomePageViewModel());
        });

        public OnboardingItemViewModel[] OnboardingItems { get; } = new[]
        {
            new OnboardingItemViewModel("Page 1", "https://i.imgur.com/hoNgoms.png"),
            new OnboardingItemViewModel("Page 2", "https://i.imgur.com/hoNgoms.png"),
            new OnboardingItemViewModel("Page 3", "https://i.imgur.com/hoNgoms.png"),
        };
    }
}
