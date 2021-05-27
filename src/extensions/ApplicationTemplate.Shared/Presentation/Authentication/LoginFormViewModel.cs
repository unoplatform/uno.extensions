using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
//using Chinook.DynamicMvvm;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ApplicationTemplate.Presentation
{
    public class LoginFormViewModel : ViewModel
    {
        //public LoginFormViewModel()
        //{
        //    this.AddValidation(this.GetProperty(x => x.Email));
        //    this.AddValidation(this.GetProperty(x => x.Password));
        //}

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

    public class LoginFormValidator : AbstractValidator<LoginFormViewModel>
    {
        public LoginFormValidator(IStringLocalizer localizer)
        {
            // This is an example of overriding one specific validation error message
            RuleFor(x => x.Email).NotEmpty().WithMessage(_ => localizer["ValidationNotEmpty_Email"]).EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
