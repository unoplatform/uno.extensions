namespace Uno.Extensions.Validation;

internal class Validator : IValidator
{
	private readonly IServiceProvider _services;
	private readonly LocalizedDataAnnotationsValidator? _localizedValidator;
	private IValidator? TypedValidator(Type instanceType) => _services.GetServices<IValidatorTypedInstance>().FirstOrDefault(x => x.InstanceType == instanceType);

	public Validator(IServiceProvider services, LocalizedDataAnnotationsValidator? localizedValidator = null)
	{
		_services = services;
		_localizedValidator = localizedValidator;
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
				// The app services are given to the context so custom attributes and IValidatableObject can resolve services (e.g. an IStringLocalizer).
				context ??= new ValidationContext(instance, _services, items: null);
				bool validates = _localizedValidator?.TryValidateObject(instance, context, results)
					?? System.ComponentModel.DataAnnotations.Validator.TryValidateObject(instance, context, results, true);

				if (!results.Any() && !validates && instance is INotifyDataErrorInfo _instance)
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
