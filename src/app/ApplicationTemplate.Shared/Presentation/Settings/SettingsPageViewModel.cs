using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using Chinook.DataLoader;
using Chinook.DynamicMvvm;
using Chinook.SectionsNavigation;
using Chinook.StackNavigation;
using MessageDialogService;
using Microsoft.Extensions.Localization;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace ApplicationTemplate.Presentation
{
	public partial class SettingsPageViewModel : ViewModel
	{
		public string VersionNumber => this.Get(GetVersionNumber);

		public IDataLoader<UserProfileData> UserProfile => this.GetDataLoader(GetUserProfile, db => db
			.TriggerFromObservable(this.GetService<IAuthenticationService>().GetAndObserveIsAuthenticated().Skip(1))
		);

		public IDynamicCommand Logout => this.GetCommandFromTask(async ct =>
		{
			var logout = await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
				.TitleResource("Logout_Title")
				.ContentResource("Logout_Content")
				.CancelCommand()
				.AcceptCommand("Logout_Confirm")
			);

			if (logout == MessageDialogResult.Accept)
			{
				await this.GetService<IAuthenticationService>().Logout(ct);
			}
		});

		public IDynamicCommand NavigateToDiagnosticsPage => this.GetCommandFromTask(async ct =>
		{
			await this.GetService<ISectionsNavigator>().OpenModal(ct, () => new DiagnosticsPageViewModel());
		});

		public IDynamicCommand NavigateToEditProfilePage => this.GetCommandFromTask(async ct =>
		{
			var userProfile = UserProfile.State.Data;

			await this.GetService<IStackNavigator>().Navigate(ct, () => new EditProfilePageViewModel(userProfile));
		});

		public IDynamicCommand NavigateToLoginPage => this.GetCommandFromTask(async ct =>
		{
			await this.GetService<IStackNavigator>().Navigate(ct, () => new LoginPageViewModel(async ct2 =>
			{
				await this.GetService<ISectionsNavigator>().NavigateBackOrCloseModal(ct2);
			}));
		});

		public IDynamicCommand NavigateToLicensesPage => this.GetCommandFromTask(async ct =>
		{
			await this.GetService<IStackNavigator>().Navigate(ct, () => new LicensesPageViewModel());
		});

		public IDynamicCommand NavigateToPrivacyPolicyPage => this.GetCommandFromTask(async ct =>
		{
			var url = this.GetService<IStringLocalizer>()["PrivacyPolicyUrl"];

			await this.GetService<IBrowser>().OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
		});

		public IDynamicCommand NavigateToTermsAndConditionsPage => this.GetCommandFromTask(async ct =>
		{
			var url = this.GetService<IStringLocalizer>()["TermsAndConditionsUrl"];

			await this.GetService<IBrowser>().OpenAsync(new Uri(url), BrowserLaunchMode.External);
		});

		public IDynamicCommand NavigateToWebViewPage => this.GetCommandFromTask(async ct =>
		{
			await this.GetService<IStackNavigator>().Navigate(ct, () => new WebViewPageViewModel("Nventive", new Uri("http://www.nventive.com")));
		});

		private async Task<UserProfileData> GetUserProfile(CancellationToken ct)
		{
			return await this.GetService<IUserProfileService>().GetCurrent(ct);
		}

		private string GetVersionNumber()
		{
			return this.GetService<IAppInfo>().VersionString;
		}
	}
}
