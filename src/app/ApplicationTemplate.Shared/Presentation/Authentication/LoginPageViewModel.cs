using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
    public class LoginPageViewModel : ViewModel
    {
        private readonly Func<CancellationToken, Task> _onSuccessfulLogin;

        public LoginPageViewModel(Func<CancellationToken, Task> onSuccessfulLogin)
        {
            _onSuccessfulLogin = onSuccessfulLogin ?? throw new ArgumentNullException(nameof(onSuccessfulLogin));
        }

        public LoginFormViewModel Form => this.GetChild(() => new LoginFormViewModel());

        public IDynamicCommand Login => this.GetCommandFromTask(async ct =>
        {
            var validationResult = await Form.Validate(ct);

            if (validationResult.IsValid)
            {
                await this.GetService<IAuthenticationService>().Login(ct, Form.Email.Trim(), Form.Password);

                await _onSuccessfulLogin.Invoke(ct);
            }
        });

        public IDynamicCommand NavigateToCreateAccountPage => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new CreateAccountPageViewModel());
        });

        public IDynamicCommand NavigateToForgotPasswordPage => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new ForgotPasswordPageViewModel());
        });
    }
}
