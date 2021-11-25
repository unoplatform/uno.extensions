using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal class MappedField : IMappedMember
{
	private readonly IFieldSymbol _field;

	public MappedField(IFieldSymbol field)
	{
		_field = field;
	}

	/// <inheritdoc />
	public string GetDeclaration()
		=> $@"{_field.GetAccessibilityAsCSharpCodeString()} {_field.Type} {_field.Name}
			{{
				get => {N.Model}.{_field.Name};
				set => {N.Model}.{_field.Name} = value;
			}}";

	/// <inheritdoc />
	public virtual string? GetInitialization()
		=> null;
}
