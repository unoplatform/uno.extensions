using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal class MappedProperty : IMappedMember
{
	private readonly IPropertySymbol _property;

	public MappedProperty(IPropertySymbol property)
	{
		_property = property;
	}

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
