using System;
using System.Collections.Generic;
using System.Text;
using Chinook.DynamicMvvm;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ApplicationTemplate.Presentation
{
	public class LoginFormViewModel : ViewModel
	{
		public LoginFormViewModel()
		{
			this.AddValidation(this.GetProperty(x => x.Email));
			this.AddValidation(this.GetProperty(x => x.Password));
		}

		public string Email
		{
			get => this.Get<string>();
			set => this.Set(value);
		}

		public string Password
		{
			get => this.Get<string>();
			set => this.Set(value);
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
