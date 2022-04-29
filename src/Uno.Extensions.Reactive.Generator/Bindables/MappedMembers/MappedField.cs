using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record MappedField(IFieldSymbol _field) : IMappedMember
{
	private readonly IFieldSymbol _field = _field;

	/// <inheritdoc />
	public string Name => _field.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

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
