namespace Uno.Extensions.Edition;

/// <summary>
/// A marker attribute for lambdas that are used as property selectors.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PropertySelectorAttribute : Attribute
{
}
