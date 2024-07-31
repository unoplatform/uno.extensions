using Uno.Extensions.Validation;
using TestHarness.Ext.Navigation.Validation;

namespace TestHarness;

public class ValidationHostInit : BaseHostInitialization
{
	protected override IHostBuilder Custom(IHostBuilder builder)
		=>builder
			.UseValidation(configure:
			 validationBuilder=>
				validationBuilder.Validator<ValidationUser,ValidationUserValidator>());

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}
