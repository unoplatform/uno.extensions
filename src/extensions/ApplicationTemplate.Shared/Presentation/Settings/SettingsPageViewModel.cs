using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Input;
//using Chinook.DataLoader;
//using Chinook.DynamicMvvm;
//using Chinook.SectionsNavigation;
//using Chinook.StackNavigation;
//using MessageDialogService;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace ApplicationTemplate.Presentation
{
    public partial class SettingsPageViewModel : ViewModel
    {
        private IUserProfileService UserProfileService { get; }
        private IAppInfo AppInfo { get; }
        private IRouteMessenger Messenger { get; }
        private IAuthenticationService AuthService { get; }

        public SettingsPageViewModel(
            //IUserProfileService userProfileService,
            //IAppInfo appInfo,
            IAuthenticationService authService,
            IRouteMessenger messenger)
        {
            //UserProfileService = userProfileService;
            //AppInfo = appInfo;
            Messenger = messenger;
            AuthService = authService;

            //LoadData();
            //VersionNumber = appInfo.VersionString;
        }

        private async void LoadData()
        {
            var cancel = new CancellationTokenSource();
            UserProfile = await UserProfileService.GetCurrent(cancel.Token);

        }

        public string VersionNumber { get; }// => this.Get(GetVersionNumber);

        private UserProfileData userProfile;
        public UserProfileData UserProfile {
            get => userProfile;
            set => SetProperty(ref userProfile, value);
        }// => this.GetDataLoader(GetUserProfile, db => db
        //public IDataLoader<UserProfileData> UserProfile => this.GetDataLoader(GetUserProfile, db => db
        //    .TriggerFromObservable(this.GetService<IAuthenticationService>().GetAndObserveIsAuthenticated().Skip(1))
        //);

        //public IDynamicCommand Logout => this.GetCommandFromTask(async ct =>
        //{
        //    var logout = await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
        //        .TitleResource("Logout_Title")
        //        .ContentResource("Logout_Content")
        //        .CancelCommand()
        //        .AcceptCommand("Logout_Confirm")
        //    );

        //    if (logout == MessageDialogResult.Accept)
        //    {
        //        await this.GetService<IAuthenticationService>().Logout(ct);
        //    }
        //});
        public ICommand Logout => new AsyncRelayCommand(async () =>
        {
            var cancel = new CancellationTokenSource();
            await AuthService.Logout(cancel.Token);
        });

        //public IDynamicCommand NavigateToDiagnosticsPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<ISectionsNavigator>().OpenModal(ct, () => new DiagnosticsPageViewModel());
        //});
        public ICommand NavigateToDiagnosticsPage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(DiagnosticsPageViewModel).AsRoute())));

        //public IDynamicCommand NavigateToEditProfilePage => this.GetCommandFromTask(async ct =>
        //{
        //    var userProfile = UserProfile.State.Data;

        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new EditProfilePageViewModel(userProfile));
        //});
        public ICommand NavigateToEditProfilePage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(EditProfilePageViewModel).AsRoute())));

        //public IDynamicCommand NavigateToLoginPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new LoginPageViewModel(async ct2 =>
        //    {
        //        await this.GetService<ISectionsNavigator>().NavigateBackOrCloseModal(ct2);
        //    }));
        //});
        public ICommand NavigateToLoginPage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(LoginPageViewModel).AsRoute())));

        //public IDynamicCommand NavigateToLicensesPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new LicensesPageViewModel());
        //});

        public ICommand NavigateToLicensesPage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(LicensesPageViewModel).AsRoute())));

        //public IDynamicCommand NavigateToPrivacyPolicyPage => this.GetCommandFromTask(async ct =>
        //{
        //    var url = this.GetService<IStringLocalizer>()["PrivacyPolicyUrl"];

        //    await this.GetService<IBrowser>().OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
        //});


        //public IDynamicCommand NavigateToTermsAndConditionsPage => this.GetCommandFromTask(async ct =>
        //{
        //    var url = this.GetService<IStringLocalizer>()["TermsAndConditionsUrl"];

        //    await this.GetService<IBrowser>().OpenAsync(new Uri(url), BrowserLaunchMode.External);
        //});


        //public IDynamicCommand NavigateToWebViewPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new WebViewPageViewModel("Nventive", new Uri("http://www.nventive.com")));
        //});

        //private async Task<UserProfileData> GetUserProfile(CancellationToken ct)
        //{
        //    return await this.GetService<IUserProfileService>().GetCurrent(ct);
        //}

        //private string GetVersionNumber()
        //{
        //    return this.GetService<IAppInfo>().VersionString;
        //}
    }
}
