namespace Uno.Extensions.Validation;

/// <summary>
/// Defines an interface for a data validator.
/// </summary>
/// <typeparam name="T">Instance to validate</typeparam>
internal interface IValidator<in T> : IValidator { }
