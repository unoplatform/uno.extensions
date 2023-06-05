namespace Uno.Extensions.Validation;

internal interface IValidatorTypedInstance : IValidator
{
	Type InstanceType { get; }
}
