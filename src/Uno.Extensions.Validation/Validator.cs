namespace Uno.Extensions.Validation;

internal class Validator : IValidator
{
	private readonly IServiceProvider _services;
	private IValidator? TypedValidator(Type instanceType) => _services.GetServices<IValidatorTypedInstance>().FirstOrDefault(x => x.InstanceType == instanceType);

	public Validator(IServiceProvider services)
	{
		_services = services;
	}

	public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
		object instance,
		ValidationContext? context = null,
		CancellationToken cancellationToken = default)
	{
		var validator = TypedValidator(instance.GetType());
		if (validator != null)
		{
			var results = await validator.ValidateAsync(instance, context, cancellationToken);
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
					return _instance?.GetErrors(null).OfType<ValidationResult>()?.ToList()
						?? new List<ValidationResult>();
				}
			}
			catch { }

			return results;
		}
	}
}
