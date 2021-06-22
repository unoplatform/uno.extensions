using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
//using Chinook.DynamicMvvm;
// using FluentValidation;

namespace ApplicationTemplate.Presentation
{
    public class CreateAccountFormViewModel : ViewModel
    {
        //public CreateAccountFormViewModel()
        //{
        //    this.AddValidation(this.GetProperty(x => x.Email));
        //    this.AddValidation(this.GetProperty(x => x.Password));
        //}

        private string email;

        //[Required]
        public string Email
        {
            get => email;
            set => SetProperty(ref email,value, true);
        }

        private string password;

        //[Required]
        public string Password
        {
            get => password;
            set => SetProperty(ref password,  value, true);
        }
    }

    //public class CreateAccountFormValidator : AbstractValidator<CreateAccountFormViewModel>
    //{
    //    public CreateAccountFormValidator()
    //    {
    //        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    //        RuleFor(x => x.Password).NotEmpty();
    //    }
    //}
}
