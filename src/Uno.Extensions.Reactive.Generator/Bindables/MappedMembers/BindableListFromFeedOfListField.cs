using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFromFeedOfListField(IFieldSymbol Field, ITypeSymbol CollectionType, ITypeSymbol ItemType) : IMappedMember
{
	/// <inheritdoc />
	public string Name => Field.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{Field.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{ItemType}> {Field.Name};"; // Note: This should be a State

	/// <inheritdoc />
	public string? GetInitialization()
		=> @$"
			var {Field.GetCamelCaseName()}Source = {N.Ctor.Model}.{Field.Name} ?? throw new NullReferenceException(""The list feed field '{Field.Name}' is null. Public feeds properties must be initialized in the constructor."");
			var {Field.GetCamelCaseName()}SourceListFeed = {N.ListFeed.Extensions.ToListFeed}<{CollectionType}, {ItemType}>({Field.GetCamelCaseName()}Source);
			{Field.Name} = new {NS.Bindings}.BindableListFeed<{ItemType}>(nameof({Field.Name}), {Field.GetCamelCaseName()}SourceListFeed, {N.Ctor.Ctx});";
}
