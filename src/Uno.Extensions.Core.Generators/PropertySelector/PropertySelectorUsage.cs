using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

/// <summary>
/// An invocation of a method that has one or more PropertySelector parameter
/// </summary>
internal readonly record struct PropertySelectorUsage(IMethodSymbol Method, FileLinePositionSpan Location, IImmutableList<PropertySelectorUsageItem> Items);

internal readonly record struct PropertySelectorUsageItem(IParameterSymbol Parameter, ArgumentSyntax Argument, string Key);
