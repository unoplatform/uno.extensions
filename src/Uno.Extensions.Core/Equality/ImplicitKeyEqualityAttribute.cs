using System;
using System.Linq;

namespace Uno.Extensions.Equality;

/// <summary>
/// Configures the generation of implicit key equality comparer for entry tracking.
/// See remarks for details about key resolution.
/// </summary>
/// <remarks>
/// When key equality generator runs, it will apply those rules in that order:
///  1. If one or more properties are flagged with the <see cref="KeyAttribute"/>, those properties will be used.
///  2. Otherwise, if a property is named like one of the configured <see cref="ImplicitKeyEqualityAttribute.PropertyNames"/>, this property is going to be used.
///		Matching is made case-insensitive.
///		If there are multiple properties that are matching, the order of the configured <see cref="ImplicitKeyEqualityAttribute.PropertyNames"/> will be used,
///		and only the first one witch match will be used.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ImplicitKeyEqualityAttribute : Attribute
{
	/// <summary>
	/// Gets or sets a bool which indicates if the generation of key equality based on property names is enabled of not.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// The name of properties that should be implicitly used as key.
	/// </summary>
	public string[] PropertyNames { get; } = { "Id", "Key" };

	/// <summary>
	/// Create a new instance using default values.
	/// </summary>
	public ImplicitKeyEqualityAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance specifying the <see cref="PropertyNames"/>.
	/// </summary>
	/// <param name="propertyNames">The name of properties that should be implicitly used as key.</param>
	public ImplicitKeyEqualityAttribute(params string[] propertyNames)
	{
		PropertyNames = propertyNames;
	}
}
