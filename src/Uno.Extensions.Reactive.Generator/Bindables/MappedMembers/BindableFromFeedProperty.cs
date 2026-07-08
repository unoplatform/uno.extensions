using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableFromFeedProperty(IPropertySymbol _property, ITypeSymbol _valueType, string? _bindableValueType) : IMappedMember
{
	private readonly IPropertySymbol _property = _property;
	private readonly ITypeSymbol _valueType = _valueType;
	private readonly string _bindableValueType = _bindableValueType ?? $"{NS.Bindings}.Bindable<{_valueType.ToFullString()}>";

	/// <inheritdoc />
	public string Name => _property.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration() =>
		$$"""
		{{_property.GetAccessibilityAsCSharpCodeString()}} {{_bindableValueType}} {{_property.Name}}
		{
			[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof({{_bindableValueType}}))]
			get;
			private set;
		}
		""";

	/// <inheritdoc />
	public string? GetInitialization()
		=> $"{_property.Name} ??= new {_bindableValueType}(base.Property<{_valueType.ToFullString()}>(nameof({_property.Name}), ({NS.Reactive}.IFeed<{_valueType.ToFullString()}>) {N.Ctor.Model}.{_property.Name} ?? throw new NullReferenceException(\"The feed field '{_property.Name}' is null. Public feeds fields must be initialized in the constructor.\")));";
}
