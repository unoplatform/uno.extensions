using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationTemplate.Business;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public class CreateAccountPageViewModel : ViewModel
    {
        public CreateAccountFormViewModel Form { get; }// => this.GetChild(() => new CreateAccountFormViewModel());

        private IAuthenticationService AuthService { get; }
        private IRouteMessenger Messenger { get; }

        public CreateAccountPageViewModel(CreateAccountFormViewModel form, IAuthenticationService authService, IRouteMessenger messenger)
        {
            Form = form;
            AuthService = authService;
            Messenger = messenger;
        }

        //public IDynamicCommand CreateAccount => this.GetCommandFromTask(async ct =>
        //{
        //    var validationResult = await Form.Validate(ct);

        //    if (validationResult.IsValid)
        //    {
        //        await this.GetService<IAuthenticationService>().CreateAccount(ct, Form.Email.Trim(), Form.Password);

        //        await this.GetService<IStackNavigator>().NavigateAndClear(ct, () => new HomePageViewModel());
        //    }
        //});

        public ICommand CreateAccount => new AsyncRelayCommand(async () => {
            var hasNoErrors = this.Form.Validate();
            if (hasNoErrors)
            {
                var cancel = new CancellationTokenSource();
                await AuthService.CreateAccount(cancel.Token, Form.Email, Form.Password);
                Messenger.Send(new ClearStackMessage(this, typeof(HomePageViewModel).Name.ToString()));
            }
            });
    }
}
