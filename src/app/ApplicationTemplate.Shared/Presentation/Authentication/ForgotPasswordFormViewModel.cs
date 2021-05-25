using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
//using Chinook.DynamicMvvm;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
    public class ForgotPasswordFormViewModel : ViewModel
    {
        //public ForgotPasswordFormViewModel()
        //{
        //    this.AddValidation(this.GetProperty(x => x.Email));
        //}

        private string email;

        [Required]
        public string Email
        {
            get => email;
            set => SetProperty(ref email, value, true);
        }
    }

    public class ForgotPasswordFormValidator : AbstractValidator<ForgotPasswordFormViewModel>
    {
        public ForgotPasswordFormValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}
