using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels
{
	public class ProfileViewModel
	{
		private readonly IProfileService _profileService;
		//private Profile _person;
		public ProfileViewModel(IProfileService profileService)
		{
			//_person = new Profile { FirstName = "Fred", LastName = "Jobs" };
			_profileService = profileService;
		}

		public IFeed<ProfileModel> Profile => Feed.Async<ProfileModel>(async ct => new ProfileModel(await _profileService.GetProfile(ct)));
		//public string FullName => $"{_person.FirstName} {_person.LastName}";
	}
}
