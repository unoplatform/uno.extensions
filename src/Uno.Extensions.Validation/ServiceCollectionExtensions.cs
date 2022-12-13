namespace Uno.Extensions.Validation;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInstanceTypeInfo<TEntity>(
	this IServiceCollection services
	)where TEntity : class
	{
		return services
			.AddSingleton<IInstanceType>(sp => new IInstanceTypeWrapper<TEntity>(sp));
		
	}
}
