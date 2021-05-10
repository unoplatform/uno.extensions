using System;
using System.Collections.Generic;
using System.Text;
using Chinook.DynamicMvvm;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
	public class CreateAccountFormViewModel : ViewModel
	{
		public CreateAccountFormViewModel()
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

	public class CreateAccountFormValidator : AbstractValidator<CreateAccountFormViewModel>
	{
		public CreateAccountFormValidator()
		{
			RuleFor(x => x.Email).NotEmpty().EmailAddress();
			RuleFor(x => x.Password).NotEmpty();
		}
	}
}
