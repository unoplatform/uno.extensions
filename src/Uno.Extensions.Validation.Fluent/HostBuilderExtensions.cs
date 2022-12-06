namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseFluentValidation(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection> configureDelegate)
	{
		hostBuilder
	   .ConfigureServices((ctx, services) =>
	   {
		   _ = services
		   .AddScoped(typeof(IValidator<>), typeof(FluentValidator<>));
	   });

		return hostBuilder.ConfigureServices(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
	}
}
