using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Equality;

namespace Uno.Extensions.Generators.KeyEquality;

internal record KeyEqualityGenerationContext(
	GeneratorExecutionContext Context,

	// Types
	[ContextType($"{NS.Equality}.IKeyEquatable`1")] INamedTypeSymbol IKeyEquatable,
	[ContextType($"{NS.Equality}.IKeyed`1")] INamedTypeSymbol IKeyed,

	// Config
	[ContextType(typeof(ImplicitKeysAttribute))] INamedTypeSymbol ImplicitKeyAttribute,
	[ContextType(typeof(KeyAttribute))] INamedTypeSymbol KeyAttribute,
	[ContextType("System.ComponentModel.DataAnnotations.KeyAttribute?")] INamedTypeSymbol? DataAnnotationsKeyAttribute
)
{
	private IMethodSymbol? _getKeyHashCode;
	public IMethodSymbol GetKeyHashCode => _getKeyHashCode ??= IKeyEquatable.GetMethod(nameof(GetKeyHashCode));

	private IMethodSymbol? _keyEquals;
	public IMethodSymbol KeyEquals => _keyEquals ??= IKeyEquatable.GetMethod(nameof(KeyEquals));

	public ITypeSymbol ConstructTupleOrSingle(params ITypeSymbol[] typeArguments)
		=> typeArguments switch
		{
			null or { Length: 0 } => throw new InvalidOperationException("No types to create tuple."),
			{ Length: 1 } => typeArguments[0],
			_ when Context.Compilation.GetTypeByMetadataName($"System.ValueTuple`{typeArguments.Length}") is { } tuple => tuple.Construct(typeArguments),
			_ => throw new InvalidOperationException("Failed to construct tuple."),
		};

	public ImmutableArray<ITypeSymbol> DeconstructTupleOrSingle(ITypeSymbol? maybeTuple)
		=> maybeTuple switch
		{
			null => ImmutableArray<ITypeSymbol>.Empty,
			{IsTupleType: true} => ((INamedTypeSymbol)maybeTuple).TypeArguments,
			_ => ImmutableArray.Create(maybeTuple)
		};

	public INamedTypeSymbol? GetLocalIKeyed(INamedTypeSymbol? baseIKeyed, ICollection<IPropertySymbol> additionalLocalKeysTypes)
		=> (baseIKeyed, additionalLocalKeysTypes) switch
		{
			(_, null or { Count: 0 }) => null, // If no local keys, then no local implementation of IKeyed
			(null, _) => IKeyed.Construct(ConstructTupleOrSingle(additionalLocalKeysTypes.Select(k => k.Type).ToArray())),
			_ => IKeyed.Construct(ConstructTupleOrSingle(DeconstructTupleOrSingle(baseIKeyed.TypeArguments[0]).Concat(additionalLocalKeysTypes.Select(k => k.Type)).ToArray()))
		};
}
