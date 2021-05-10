using System;
using System.Collections.Generic;
using System.Text;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using Chinook.DynamicMvvm;
using Chinook.SectionsNavigation;

namespace ApplicationTemplate.Presentation
{
	public class EditProfilePageViewModel : ViewModel
	{
		private readonly UserProfileData _userProfile;

		public EditProfilePageViewModel(UserProfileData userProfile)
		{
			_userProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
		}

		public EditProfileFormViewModel Form => this.GetChild(() => new EditProfileFormViewModel(_userProfile));

		public IDynamicCommand UpdateProfile => this.GetCommandFromTask(async ct =>
		{
			var validationResult = await Form.Validate(ct);

			if (validationResult.IsValid)
			{
				var updatedUserProfile = _userProfile
					.WithFirstName(Form.FirstName)
					.WithLastName(Form.LastName);

				await this.GetService<IUserProfileService>().Update(ct, updatedUserProfile);

				await this.GetService<ISectionsNavigator>().NavigateBackOrCloseModal(ct);
			}
		});
	}
}
