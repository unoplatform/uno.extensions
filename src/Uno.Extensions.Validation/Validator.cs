using System.ComponentModel.DataAnnotations;

namespace Uno.Extensions.Validation;

public class Validator : IValidator
{
	private readonly IServiceProvider _services;
	private IInstanceType? TypedValidator(Type instanceType) => _services.GetServices<IInstanceType>().FirstOrDefault(x => x.InstanceType == instanceType);

	public Validator (IServiceProvider services)
	{
		_services = services;
	}

	public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
		object instance,
		ValidationContext? context = null,
		CancellationToken cancellationToken = default)
	{

		var type = TypedValidator(instance.GetType());
		if (type != null)
		{
			var results = await type.ValidateAsync(instance, context, cancellationToken);
			return results;
		}

		return new List<ValidationResult>();
	}
}
