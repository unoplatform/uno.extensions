using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Input;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
//using Chinook.DynamicMvvm;
//using Chinook.SectionsNavigation;

namespace ApplicationTemplate.Presentation
{
    public class EditProfilePageViewModel : ViewModel
    {
        private readonly UserProfileData _userProfile;

        public EditProfileFormViewModel Form { get; }// => this.GetChild(() => new EditProfileFormViewModel(_userProfile));

        private IUserProfileService ProfileService { get; }
        private IRouteMessenger Messenger { get; }

        public EditProfilePageViewModel(
            EditProfileFormViewModel form,
            IUserProfileService profileService,
            IRouteMessenger messenger)
        {
            Form = form;
            ProfileService = profileService;
            Messenger = messenger;

            LoadProfile();
        }

        public async void LoadProfile()
        {
            var cancel = new CancellationTokenSource();
            Form.UserProfileData = await ProfileService.GetCurrent(cancel.Token);

        }

        //public EditProfilePageViewModel(UserProfileData userProfile)
        //{
        //    _userProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
        //}


        //public IDynamicCommand UpdateProfile => this.GetCommandFromTask(async ct =>
        //{
        //    var validationResult = await Form.Validate(ct);

        //    if (validationResult.IsValid)
        //    {
        //        var updatedUserProfile = _userProfile
        //            .WithFirstName(Form.FirstName)
        //            .WithLastName(Form.LastName);

        //        await this.GetService<IUserProfileService>().Update(ct, updatedUserProfile);

        //        await this.GetService<ISectionsNavigator>().NavigateBackOrCloseModal(ct);
        //    }
        //});

        public ICommand UpdateProfile => new AsyncRelayCommand(async () =>
        {
            var hasNoErrors = Form.Validate();
            if(!hasNoErrors)
            {
                var cancel = new CancellationTokenSource();
                await ProfileService.Update(cancel.Token, Form.UserProfileData);
                Messenger.Send(new CloseMessage(this));
            }
        });
    }
}
