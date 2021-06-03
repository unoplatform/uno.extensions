using System;
using System.Windows.Input;
using ApplicationTemplate.Business;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class WelcomePageViewModel : ViewModel
    {
        public IWritableOptions<ApplicationSettings> Onboarding { get; }
        public IRouteMessenger Messenger { get; }
        public WelcomePageViewModel(
            IWritableOptions<ApplicationSettings> onboarding,
            IRouteMessenger messenger)
        {
            Onboarding = onboarding;
            Messenger = messenger;
        }

        public void ResetOnboarding()
        {
            Onboarding.Update(options => options.IsOnboardingCompleted = false);
            Messenger.Send(new ClearStackMessage(this));
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

        public ICommand NavigateToHomePage => new RelayCommand(() =>
            Messenger.Send(new RoutingMessage(this,typeof(HomePageViewModel).AsRoute() )));

        public ICommand NavigateToLoginPage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(LoginPageViewModel).AsRoute())));


        public ICommand NavigateToCreateAccountPage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(CreateAccountPageViewModel).AsRoute())));
    }
}
