using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationTemplate.Business;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public class ForgotPasswordPageViewModel : ViewModel
    {
        public ForgotPasswordFormViewModel Form { get; }//  => this.GetChild(() => new LoginFormViewModel());

        private IAuthenticationService AuthService { get; }
        private IRouteMessenger Messenger { get; }

        public ForgotPasswordPageViewModel(ForgotPasswordFormViewModel form, IAuthenticationService authService, IRouteMessenger messenger)
        {
            Form = form;
            AuthService = authService;
            Messenger = messenger;
        }

        //public ForgotPasswordFormViewModel Form => this.GetChild(() => new ForgotPasswordFormViewModel());

        //public IDynamicCommand ResetPassword => this.GetCommandFromTask(async ct =>
        //{
        //    var validationResult = await Form.Validate(ct);

        //    if (validationResult.IsValid)
        //    {
        //        await this.GetService<IAuthenticationService>().ResetPassword(ct, Form.Email.Trim());

        //        await this.GetService<IStackNavigator>().NavigateAndClear(ct, () => new HomePageViewModel());
        //    }
        //});

        public ICommand ResetPassword => new AsyncRelayCommand(async () => {
            var hasNoErrors = this.Form.Validate();
            if (hasNoErrors)
            {
                var cancel = new CancellationTokenSource();
                await AuthService.ResetPassword(cancel.Token, Form.Email.Trim());
                Messenger.Send(new ClearStackMessage(this, typeof(HomePageViewModel).AsRoute()));
            }
        });
    }
}
