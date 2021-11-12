using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive.Utils;

internal static class EnumerableExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool None<T>(this IReadOnlyCollection<T> collection)
		=> collection.Count == 0;
}
