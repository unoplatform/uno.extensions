using System;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class WelcomePageViewModel : ViewModel
    {
        public IDynamicCommand NavigateToHomePage => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new HomePageViewModel());
        });

        public IDynamicCommand NavigateToLoginPage => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new LoginPageViewModel(async ct2 =>
            {
                await this.GetService<IStackNavigator>().NavigateAndClear(ct2, () => new HomePageViewModel());
            }));
        });

        public IDynamicCommand NavigateToCreateAccountPage => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new CreateAccountPageViewModel());
        });
    }
}
