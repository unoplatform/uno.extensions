namespace Uno.Extensions.Validation;

internal class Validator : IValidator
{
	private readonly IServiceProvider _services;
	private IInstanceType? TypedValidator(Type instanceType) => _services.GetServices<IInstanceType>().FirstOrDefault(x => x.InstanceType == instanceType);

	public Validator(IServiceProvider services)
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
		else
		{
			ICollection<ValidationResult> results = new List<ValidationResult>();
			try
			{
				context ??= new ValidationContext(instance);
				System.ComponentModel.DataAnnotations.Validator.TryValidateObject(instance, context, results, true);

				if (!results.Any() && instance is INotifyDataErrorInfo _instance)
				{
					results = _instance?.GetErrors(null).OfType<ValidationResult>()?.ToList()
						?? new List<ValidationResult>();
				}
			}
			catch { }
			if (results.Any()) return results;
		}

		return new List<ValidationResult>();
	}
}
