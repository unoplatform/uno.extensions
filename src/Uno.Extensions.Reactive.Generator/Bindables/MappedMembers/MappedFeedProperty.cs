using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal class MappedFeedProperty : IMappedMember
{
	private readonly IPropertySymbol _property;
	private readonly ITypeSymbol _valueType;

	public MappedFeedProperty(IPropertySymbol property, ITypeSymbol valueType)
	{
		_property = property;
		_valueType = valueType;
	}

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{_property.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IState<{_valueType}> {_property.Name} {{ get; }}";

	/// <inheritdoc />
	public string? GetInitialization()
		=> $"{_property.Name} = {N.Ctor.Ctx}.GetOrCreateState({N.Ctor.Model}.{_property.Name} ?? throw new NullReferenceException(\"The feed property '{_property.Name}' is null. Public feeds properties must be initialized in teh constructor.\"));";
}
