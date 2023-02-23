using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

// TODO: We shouldn't have INamedTypeSymbol or ArgumentSyntax here.
// Removing INamedTypeSymbol is more important though.
internal readonly record struct PropertySelectorUsageItem(INamedTypeSymbol SelectorType, ArgumentSyntax Argument, string Key);
