using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFromListFeedProperty(IPropertySymbol _property, ITypeSymbol _valueType) : IMappedMember
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
		=> $"{_property.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{_valueType.ToFullString()}> {_property.Name} {{ get; private set; }}"; // Note: This should be a State

	/// <inheritdoc />
	public string GetInitialization()
		=> @$"
			if ({_property.Name} is null)
			{{
				var {_property.GetCamelCaseName()}Source = ({NS.Reactive}.IListFeed<{_valueType.ToFullString()}>) {N.Ctor.Model}.{_property.Name} ?? throw new NullReferenceException(""The list feed property '{_property.Name}' is null. Public feeds properties must be initialized in the constructor."");
				var {_property.GetCamelCaseName()}SourceListState = {N.Ctor.Ctx}.GetOrCreateListState({_property.GetCamelCaseName()}Source);
				{_property.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({_property.Name}), {_property.GetCamelCaseName()}SourceListState);
			}}";
}
