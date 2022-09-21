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
	public bool IsEnabled { get; init; } = true;

	public string[] PropertyNames { get; } = { "Id", "Key" };

	public ImplicitKeyEqualityAttribute()
	{
	}

	public ImplicitKeyEqualityAttribute(params string[] propertyNames)
	{
		PropertyNames = propertyNames;
	}
}
