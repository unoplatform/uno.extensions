using System;
using System.Collections.Generic;
using System.Text;
using ApplicationTemplate.Client;
using Chinook.DynamicMvvm;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
	public class EditProfileFormViewModel : ViewModel
	{
		private readonly UserProfileData _userProfileData;

		public EditProfileFormViewModel(UserProfileData userProfileData)
		{
			_userProfileData = userProfileData ?? throw new ArgumentNullException(nameof(userProfileData));

			this.AddValidation(this.GetProperty(x => x.FirstName));
			this.AddValidation(this.GetProperty(x => x.LastName));
		}

		public string FirstName
		{
			get => this.Get(_userProfileData.FirstName);
			set => this.Set(value);
		}

		public string LastName
		{
			get => this.Get(_userProfileData.LastName);
			set => this.Set(value);
		}
	}

	public class EditProfileFormValidator : AbstractValidator<EditProfileFormViewModel>
	{
		public EditProfileFormValidator()
		{
			RuleFor(x => x.FirstName).NotEmpty();
			RuleFor(x => x.LastName).NotEmpty();
		}
	}
}
