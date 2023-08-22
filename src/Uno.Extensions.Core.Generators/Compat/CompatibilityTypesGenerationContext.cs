using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators.CompatibilityTypes;

internal record CompatibilityTypesGenerationContext(
	GeneratorExecutionContext Context,

	[ContextType("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute?")] INamedTypeSymbol? DynamicallyAccessedMembersAttribute,
	[ContextType("System.Diagnostics.CodeAnalysis.MaybeNullAttribute?")] INamedTypeSymbol? MaybeNullAttribute, // .net std 2.1 and above
	[ContextType("System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute?")] INamedTypeSymbol? MaybeNullWhenAttribute, // .net std 2.1 and above
	[ContextType("System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute?")] INamedTypeSymbol? MemberNotNullWhenAttribute, // .net std 2.1 and above
	[ContextType("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute?")] INamedTypeSymbol? NotNullIfNotNullAttribute, // .net std 2.1 and above
	[ContextType("System.Diagnostics.CodeAnalysis.NotNullWhenAttribute?")] INamedTypeSymbol? NotNullWhenAttribute, // .net std 2.1 and above

	[ContextType("System.Reflection.Metadata.MetadataUpdateHandlerAttribute?")] INamedTypeSymbol? MetadataUpdateHandlerAttribute,

	[ContextType("System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute?")] INamedTypeSymbol? CreateNewOnMetadataUpdateAttribute,
	[ContextType("System.Runtime.CompilerServices.IsExternalInit?")] INamedTypeSymbol? IsExternalInit, // .net 5 and above only
	[ContextType("System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute?")] INamedTypeSymbol? MetadataUpdateOriginalTypeAttribute,
	[ContextType("System.Runtime.CompilerServices.ModuleInitializerAttribute?")] INamedTypeSymbol? ModuleInitializerAttribute // .net 5 and above only
);
