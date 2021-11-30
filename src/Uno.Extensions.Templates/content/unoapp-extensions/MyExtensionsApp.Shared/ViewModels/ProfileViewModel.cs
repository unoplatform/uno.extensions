using MyExtensionsApp.Models;
using MyExtensionsApp.Services;
using Uno.Extensions.Reactive;

namespace MyExtensionsApp.ViewModels;

public class ProfileViewModel
{
	private readonly IProfileService _profileService;
	public ProfileViewModel(IProfileService profileService)
	{
		_profileService = profileService;
	}

	public IFeed<ProfileModel> Profile => Feed
		.Async(_profileService.GetProfile)
		.Select(profile => new ProfileModel(profile));
}
