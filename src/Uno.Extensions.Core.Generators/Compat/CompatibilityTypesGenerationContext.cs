using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator.Compat;

internal record CompatibilityTypesGenerationContext(
	GeneratorExecutionContext Context,

	[ContextType("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute?")] INamedTypeSymbol? NotNullIfNotNullAttribute, // .net std 2.1 and above
	[ContextType("System.Diagnostics.CodeAnalysis.NotNullWhenAttribute?")] INamedTypeSymbol? NotNullWhenAttribute, // .net std 2.1 and above
	[ContextType("System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute?")] INamedTypeSymbol? MemberNotNullWhenAttribute, // .net std 2.1 and above

	[ContextType("System.Runtime.CompilerServices.IsExternalInit?")] INamedTypeSymbol? IsExternalInit, // .net 5 and above only
	[ContextType("System.Runtime.CompilerServices.ModuleInitializerAttribute?")] INamedTypeSymbol? ModuleInitializerAttribute // .net 5 and above only
);
