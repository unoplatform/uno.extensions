using System;
using System.Collections.Generic;
using System.Text;
using ApplicationTemplate.Client;
//using Chinook.DynamicMvvm;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
    public class EditProfileFormViewModel : ViewModel
    {
        private UserProfileData _userProfileData;
        public UserProfileData UserProfileData
        {
            get => _userProfileData;
            set
            {
                if (SetProperty(ref _userProfileData, value))
                {
                    OnPropertyChanged(nameof(FirstName));
                    OnPropertyChanged(nameof(LastName));
                }
            }
        }
        //public EditProfileFormViewModel(UserProfileData userProfileData)
        //{
        //    _userProfileData = userProfileData ?? throw new ArgumentNullException(nameof(userProfileData));

        //    this.AddValidation(this.GetProperty(x => x.FirstName));
        //    this.AddValidation(this.GetProperty(x => x.LastName));
        //}

        public string FirstName
        {
            get => UserProfileData.FirstName;
            set {
                UserProfileData.FirstName = value;
            }
        }

        public string LastName
        {
            get => UserProfileData.LastName;
            set
            {
                UserProfileData.LastName = value;
            }
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
