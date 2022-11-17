using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator.Dispatching;

internal record UIModuleInitializerGenerationContext(
	GeneratorExecutionContext Context,
	[ContextType("Uno.Extensions.Reactive.UI.ModuleInitializer?")] INamedTypeSymbol? ReactiveUIModuleInitializer);
