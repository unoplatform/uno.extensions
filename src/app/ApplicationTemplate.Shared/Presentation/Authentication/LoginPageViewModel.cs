using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationTemplate.Business;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;
//using FluentValidation;

namespace ApplicationTemplate.Presentation
{
    public class LoginPageViewModel : ViewModel
    {
        //private readonly Func<CancellationToken, Task> _onSuccessfulLogin;

        //public LoginPageViewModel(Func<CancellationToken, Task> onSuccessfulLogin)
        //{
        //    _onSuccessfulLogin = onSuccessfulLogin ?? throw new ArgumentNullException(nameof(onSuccessfulLogin));
        //}

        public LoginFormViewModel Form { get; }//  => this.GetChild(() => new LoginFormViewModel());

        private IAuthenticationService AuthService { get; }
        private IRouteMessenger Messenger { get; }

        public LoginPageViewModel(LoginFormViewModel form, IAuthenticationService authService, IRouteMessenger messenger)
        {
            Form = form;
            AuthService = authService;
            Messenger = messenger;
        }

        //public LoginFormViewModel Form => this.GetChild(() => new LoginFormViewModel());

        //public IDynamicCommand Login => this.GetCommandFromTask(async ct =>
        //{
        //    var validationResult = await Form.Validate(ct);

        //    if (validationResult.IsValid)
        //    {
        //        await this.GetService<IAuthenticationService>().Login(ct, Form.Email.Trim(), Form.Password);

        //        await _onSuccessfulLogin.Invoke(ct);
        //    }
        //});

        //public IDynamicCommand NavigateToCreateAccountPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new CreateAccountPageViewModel());
        //});

        //public IDynamicCommand NavigateToForgotPasswordPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new ForgotPasswordPageViewModel());
        //});


        public ICommand Login => new AsyncRelayCommand(async () => {
            var hasNoErrors = this.Form.Validate();
            if (hasNoErrors)
            {
                var cancel = new CancellationTokenSource();
                await AuthService.Login(cancel.Token, Form.Email, Form.Password);
                Messenger.Send(new ClearStackMessage(this, typeof(HomePageViewModel).AsRoute()));
            }
        });

        public ICommand NavigateToCreateAccountPage => new RelayCommand(() =>
   Messenger.Send(new RoutingMessage(this, typeof(CreateAccountPageViewModel).AsRoute())));

        public ICommand NavigateToForgotPasswordPage => new RelayCommand(() =>
   Messenger.Send(new RoutingMessage(this, typeof(ForgotPasswordPageViewModel).AsRoute())));
    }
}
