using System;
using System.Collections.Generic;
using System.Text;
using Chinook.DynamicMvvm;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
    public class ForgotPasswordFormViewModel : ViewModel
    {
        public ForgotPasswordFormViewModel()
        {
            this.AddValidation(this.GetProperty(x => x.Email));
        }

        public string Email
        {
            get => this.Get<string>();
            set => this.Set(value);
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
