using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFeedProperty(IPropertySymbol _property, ITypeSymbol _valueType) : IMappedMember
{
	private readonly IPropertySymbol _property = _property;
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public string Name => _property.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{_property.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{_valueType}> {_property.Name} {{ get; }}"; // Note: This should be a State

	/// <inheritdoc />
	public string GetInitialization()
		=> @$"{_property.Name} = new {NS.Bindings}.BindableListFeed<{_valueType}>(
				nameof({_property.Name}),
				{N.Ctor.Model}.{_property.Name} ?? throw new NullReferenceException(""The list feed property '{_property.Name}' is null. Public feeds properties must be initialized in the constructor.""),
				{N.Ctor.Ctx});";
}
