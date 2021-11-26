using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record MappedProperty(IPropertySymbol _property) : IMappedMember
{
	private readonly IPropertySymbol _property = _property;

	/// <inheritdoc />
	public string GetDeclaration()
		=> Property.FromProperty(_property) with
		{
			Getter = $"{N.Model}.{_property.Name}",
			Setter = $"{N.Model}.{_property.Name} = value"
		};

	/// <inheritdoc />
	public virtual string? GetInitialization()
		=> null;
}
