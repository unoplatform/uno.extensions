using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record MappedFeedProperty(IPropertySymbol _property, ITypeSymbol _valueType) : IMappedMember
{
	private readonly IPropertySymbol _property = _property;
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public string Name => _property.Name;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{_property.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IState<{_valueType}> {_property.Name} {{ get; }}";

	/// <inheritdoc />
	public string? GetInitialization()
		=> $"{_property.Name} = {N.Ctor.Ctx}.GetOrCreateState({N.Ctor.Model}.{_property.Name} ?? throw new NullReferenceException(\"The feed property '{_property.Name}' is null. Public feeds properties must be initialized in teh constructor.\"));";
}
