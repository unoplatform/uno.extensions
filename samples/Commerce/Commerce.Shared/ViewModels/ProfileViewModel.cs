using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public class ProfileViewModel
{
	private readonly INavigator _navigator;

	private readonly IWritableOptions<Credentials> _credentials;

	private readonly IProfileService _profileService;
	public ProfileViewModel(
		INavigator navigator,
		IWritableOptions<Credentials> credentials,
		IProfileService profileService)
	{
		_navigator= navigator;
		_credentials= credentials;
		_profileService = profileService;
	}

	public IFeed<ProfileModel> Profile => Feed
		.Async(_profileService.GetProfile)
		.Select(profile => new ProfileModel(profile));

	public async void Logout()
	{
		await _credentials.UpdateAsync(c => new Credentials());
		await _navigator.NavigateRouteAsync(this, "/");
	}
}
