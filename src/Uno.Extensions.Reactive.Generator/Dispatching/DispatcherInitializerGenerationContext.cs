using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator.Dispatching;

internal record DispatcherInitializerGenerationContext(
	GeneratorExecutionContext Context,
	[ContextType("Uno.Extensions.Reactive.Dispatching.DispatcherQueueProvider?")] INamedTypeSymbol? DispatcherProvider);
