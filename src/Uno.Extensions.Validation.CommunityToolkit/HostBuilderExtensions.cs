using System;
using System.ComponentModel;

namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	private static bool _isRegistered;
	public static IHostBuilder UseCommunityToolkitValidation(
			this IHostBuilder hostBuilder,
			Action<HostBuilderContext, IServiceCollection>? configureDelegate = default)
	{
		if (_isRegistered)
		{
			return hostBuilder;
		}
		_isRegistered = true;
		hostBuilder
		.ConfigureServices((ctx, services) =>
		{
			_ = services
			//.AddScoped(typeof(IValidator<>), typeof(CommunityToolkitValidator<>))
			.AddScoped<IValidator, Validator>();
		});

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
