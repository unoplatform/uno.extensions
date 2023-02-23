using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators.PropertySelector;

/// <summary>
/// The resolved path (.A.B.C) of a PropertySelector
/// </summary>
internal readonly record struct PropertySelectorPath(string FullPath, IList<PropertySelectorPathPart> Parts);

internal readonly record struct PropertySelectorPathPart(string Name, string Accessor);
