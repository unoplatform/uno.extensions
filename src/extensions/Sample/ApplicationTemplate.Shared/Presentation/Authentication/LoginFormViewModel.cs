using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
//using Chinook.DynamicMvvm;
// using FluentValidation;
using Microsoft.Extensions.Localization;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Localization;

namespace ApplicationTemplate.Presentation
{
    public class LoginFormViewModel : ViewModel
    {
        private readonly IStringLocalizer _localizer;
        public LoginFormViewModel(
            IStringLocalizer localizer,
            IWritableOptions<LocalizationSettings> localization)
        {
            _localizer = localizer;
            var validation = _localizer["ValidationNotEmpty_Email"];
            //    this.AddValidation(this.GetProperty(x => x.Email));
            //    this.AddValidation(this.GetProperty(x => x.Password));

            localization.Update(settings =>
            {
                var current = settings.CurrentCulture;
                var index = current!=null?settings.Cultures.IndexOf(current):-1;
                settings.CurrentCulture = settings.Cultures[(++index) % settings.Cultures.Length];
            });
        }

        private string email;

        //[Required]
        public string Email
        {
            get => email;
            set => SetProperty(ref email, value, true);
        }

        private string password;

        //[Required]
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value, true);
        }
    }

    //public class LoginFormValidator : AbstractValidator<LoginFormViewModel>
    //{
    //    public LoginFormValidator(IStringLocalizer localizer)
    //    {
    //        // This is an example of overriding one specific validation error message
    //        RuleFor(x => x.Email).NotEmpty().WithMessage(_ => localizer["ValidationNotEmpty_Email"]).EmailAddress();
    //        RuleFor(x => x.Password).NotEmpty();
    //    }
    //}
}
