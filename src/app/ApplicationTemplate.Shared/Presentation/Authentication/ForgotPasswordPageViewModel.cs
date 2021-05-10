using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
	public class ForgotPasswordPageViewModel : ViewModel
	{
		public ForgotPasswordFormViewModel Form => this.GetChild(() => new ForgotPasswordFormViewModel());

		public IDynamicCommand ResetPassword => this.GetCommandFromTask(async ct =>
		{
			var validationResult = await Form.Validate(ct);

			if (validationResult.IsValid)
			{
				await this.GetService<IAuthenticationService>().ResetPassword(ct, Form.Email.Trim());

				await this.GetService<IStackNavigator>().NavigateAndClear(ct, () => new HomePageViewModel());
			}
		});
	}
}
