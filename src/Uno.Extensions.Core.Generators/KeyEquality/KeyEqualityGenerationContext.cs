using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Equality;

namespace Uno.Extensions.Reactive.Generator;

internal record KeyEqualityGenerationContext(
	GeneratorExecutionContext Context,

	// Types
	[ContextType($"{NS.Equality}.IKeyEquatable`1")] INamedTypeSymbol IKeyEquatable,

	// Config
	[ContextType(typeof(ImplicitKeyEqualityAttribute))] INamedTypeSymbol ImplicitKeyAttribute,
	[ContextType(typeof(KeyAttribute))] INamedTypeSymbol KeyAttribute,
	[ContextType("System.ComponentModel.DataAnnotations.KeyAttribute?")] INamedTypeSymbol? DataAnnotationsKeyAttribute
)
{
	private IMethodSymbol? _getKeyHashCode;
	public IMethodSymbol GetKeyHashCode => _getKeyHashCode ??= IKeyEquatable.GetMethod(nameof(GetKeyHashCode));

	private IMethodSymbol? _keyEquals;
	public IMethodSymbol KeyEquals => _keyEquals ??= IKeyEquatable.GetMethod(nameof(KeyEquals));
}
