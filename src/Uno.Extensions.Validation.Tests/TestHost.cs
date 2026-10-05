namespace Uno.Extensions.Validation.Tests;

internal static class TestHost
{
	public static IHost Create(Func<IValidationBuilder, IHostBuilder>? configure = null, Action<IServiceCollection>? services = null)
		=> new HostBuilder()
			.ConfigureServices(s => services?.Invoke(s))
			.UseValidation(configure: configure)
			.Build();

	public static IValidator GetValidator(this IHost host)
		=> host.Services.GetRequiredService<IValidator>();
}
