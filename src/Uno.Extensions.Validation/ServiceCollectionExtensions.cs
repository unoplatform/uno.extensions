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

internal interface IInstanceType : IValidator
{
	Type InstanceType { get; }
}

internal record IInstanceTypeWrapper<T>(IServiceProvider Services) : IInstanceType
{
	public Type InstanceType => typeof(T);
	public IValidator<T> Validator => Services.GetRequiredService<IValidator<T>>();
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(object instance, ValidationContext? context = null, CancellationToken cancellationToken = default) => Validator.ValidateAsync(instance, context, cancellationToken);
}
