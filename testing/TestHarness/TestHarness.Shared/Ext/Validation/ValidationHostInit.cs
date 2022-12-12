using Uno.Extensions.Validation;

namespace TestHarness;

public class ValidationHostInit : BaseHostInitialization
{
	protected override IHostBuilder Custom(IHostBuilder builder)
		=>builder
			.UseValidation()
			.UseCommunityToolkitValidation()
			.UseFluentValidation(
			configureDelegate: (ctx,services)=>
			{
				services.RegisterValidator<
					TestHarness.Ext.Navigation.Validation.ValidationUser,
					TestHarness.Ext.Navigation.Validation.ValidationUserValidator>();
			});

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}
