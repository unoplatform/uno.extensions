using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Uno.Extensions.Reactive.Utils;

internal static class OptionExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IAsyncEnumerable<Option<T>> SomeOrNoneWhenNotNull<T>(this IAsyncEnumerable<T> enumerable)
		=> enumerable.Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IAsyncEnumerable<Option<T>> SomeOrNone<T>(this IAsyncEnumerable<T?> enumerable)
		=> enumerable.Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<Option<T>> SomeOrNoneWhenNotNull<T>(this Func<T> func)
		=> () => Option.SomeOrNone(func());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<Option<T>> SomeOrNone<T>(this Func<T?> func)
		=> () => Option.SomeOrNone(func());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<Option<T>> SomeOrNone<T>(this Func<T?> func)
		where T : struct
		=> () => Option.SomeOrNone(func());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>> SomeOrNoneWhenNotNull<T>(this AsyncFunc<T> func)
		=> async ct => Option.SomeOrNone(await func(ct));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>> SomeOrNone<T>(this AsyncFunc<T?> func)
		=> async ct => Option.SomeOrNone(await func(ct));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>> SomeOrNone<T>(this AsyncFunc<T?> func)
		where T : struct
		=> async ct => Option.SomeOrNone(await func(ct));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<CancellationToken, IAsyncEnumerable<Option<T>>> SomeOrNoneWhenNotNull<T>(this Func<CancellationToken, IAsyncEnumerable<T>> factory)
		=> ct => factory(ct).Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<CancellationToken, IAsyncEnumerable<Option<T>>> SomeOrNone<T>(this Func<CancellationToken, IAsyncEnumerable<T?>> factory)
		=> ct => factory(ct).Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<CancellationToken, IAsyncEnumerable<Option<T>>> SomeOrNone<T>(this Func<CancellationToken, IAsyncEnumerable<T?>> factory)
		where T : struct
		=> ct => factory(ct).Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<CancellationToken, IAsyncEnumerable<Option<T>>> SomeOrNoneWhenNotNull<T>(this Func<IAsyncEnumerable<T>> factory)
		=> ct => factory().Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<CancellationToken, IAsyncEnumerable<Option<T>>> SomeOrNone<T>(this Func<IAsyncEnumerable<T?>> factory)
		=> ct => factory().Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<CancellationToken, IAsyncEnumerable<Option<T>>> SomeOrNone<T>(this Func<IAsyncEnumerable<T?>> factory)
		where T : struct
		=> ct => factory().Select(Option.SomeOrNone);
}
