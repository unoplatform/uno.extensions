using System;
using System.ComponentModel;

namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseCommunityToolkitValidation(
			this IHostBuilder hostBuilder,
			Action<HostBuilderContext, IServiceCollection>? configureDelegate = default)
	{
		hostBuilder
		.UseValidation();

		return configureDelegate is not null ? hostBuilder.ConfigureServices(configureDelegate) : hostBuilder;
	}

	public static IServiceCollection RegisterObservableValidator<TEntity>(
	this IServiceCollection services)
	where TEntity : ObservableValidator
	{
		return services
		.AddScoped(typeof(IValidator<TEntity>), typeof(CommunityToolkitValidator<TEntity>))
		.AddInstanceTypeInfo<TEntity>();
	}
}
