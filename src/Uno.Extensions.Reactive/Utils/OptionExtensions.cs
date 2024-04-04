using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal static class OptionExtensions
{
	#region Func
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
	public static Func<Option<T>, Option<TResult>> SomeOrNoneWhenNotNull<T, TResult>(this Func<T, TResult> func)
		=> d => d.MapToSomeOrNoneWhenNotNull(func);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<Option<T>, Option<TResult>> SomeOrNone<T, TResult>(this Func<T, TResult?> func)
		=> d => d.MapToSomeOrNone(func);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<Option<T>, Option<TResult>> SomeOrNone<T, TResult>(this Func<T, TResult?> func)
		where TResult : struct
		=> d => d.MapToSomeOrNone(func);
	#endregion

	#region AsyncAction
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncAction<Option<T>> SomeOrNone<T>(this AsyncAction<T?> func)
		=> async (t, ct) => await func(t.SomeOrDefault(), ct).ConfigureAwait(false);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncAction<Option<T?>> SomeOrNone<T>(this AsyncAction<T?> func)
		where T : struct
		=> async (t, ct) => await func(t.SomeOrDefault(), ct).ConfigureAwait(false);
	#endregion

	#region AsyncFunc
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>> SomeOrNoneWhenNotNull<T>(this AsyncFunc<T> func)
		=> async ct => Option.SomeOrNone(await func(ct).ConfigureAwait(false));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>> SomeOrNone<T>(this AsyncFunc<T?> func)
		=> async ct => Option.SomeOrNone(await func(ct).ConfigureAwait(false));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>> SomeOrNone<T>(this AsyncFunc<T?> func)
		where T : struct
		=> async ct => Option.SomeOrNone(await func(ct).ConfigureAwait(false));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>, Option<TResult>> SomeOrNoneWhenNotNull<T, TResult>(this AsyncFunc<T, TResult> func)
		=> (d, ct) => d.MapToSomeOrNoneWhenNotNullAsync(func, ct);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>, Option<TResult>> SomeOrNone<T, TResult>(this AsyncFunc<T, TResult?> func)
		=> (d, ct) => d.MapToSomeOrNoneAsync(func, ct);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AsyncFunc<Option<T>, Option<TResult>> SomeOrNone<T, TResult>(this AsyncFunc<T, TResult?> func)
		where TResult : struct
		=> (d, ct) => d.MapToSomeOrNoneAsync(func, ct);
	#endregion

	#region AsyncEnumerable
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IAsyncEnumerable<Option<T>> SomeOrNoneWhenNotNull<T>(this IAsyncEnumerable<T> enumerable)
		=> enumerable.Select(Option.SomeOrNone);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IAsyncEnumerable<Option<T>> SomeOrNone<T>(this IAsyncEnumerable<T?> enumerable)
		=> enumerable.Select(Option.SomeOrNone); 

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
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Option<TResult> MapToSomeOrNoneWhenNotNull<T, TResult>(this Option<T> data, Func<T, TResult> projection)
		=> data.Type switch
		{
			OptionType.Undefined => Option<TResult>.Undefined(),
			OptionType.None => Option<TResult>.None(),
			OptionType.Some when projection(data.SomeOrDefault()!) is TResult result => result,
			OptionType.Some => Option<TResult>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Option<TResult> MapToSomeOrNone<T, TResult>(this Option<T> data, Func<T, TResult?> projection)
		=> data.Type switch
		{
			OptionType.Undefined => Option<TResult>.Undefined(),
			OptionType.None => Option<TResult>.None(),
			OptionType.Some when projection(data.SomeOrDefault()!) is TResult result => result,
			OptionType.Some => Option<TResult>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Option<TResult> MapToSomeOrNone<T, TResult>(this Option<T> data, Func<T, TResult?> projection)
		where TResult : struct
		=> data.Type switch
		{
			OptionType.Undefined => Option<TResult>.Undefined(),
			OptionType.None => Option<TResult>.None(),
			OptionType.Some when projection(data.SomeOrDefault()!) is TResult result => result,
			OptionType.Some => Option<TResult>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask<Option<TResult>> MapToSomeOrNoneWhenNotNullAsync<T, TResult>(this Option<T> data, AsyncFunc<T, TResult> projection, CancellationToken ct)
		=> data.Type switch
		{
			OptionType.Undefined => Option<TResult>.Undefined(),
			OptionType.None => Option<TResult>.None(),
			OptionType.Some when await projection(data.SomeOrDefault()!, ct).ConfigureAwait(false) is TResult result => result,
			OptionType.Some => Option<TResult>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask<Option<TResult>> MapToSomeOrNoneAsync<T, TResult>(this Option<T> data, AsyncFunc<T, TResult?> projection, CancellationToken ct)
		=> data.Type switch
		{
			OptionType.Undefined => Option<TResult>.Undefined(),
			OptionType.None => Option<TResult>.None(),
			OptionType.Some when await projection(data.SomeOrDefault()!, ct).ConfigureAwait(false) is TResult result => result,
			OptionType.Some => Option<TResult>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask<Option<TResult>> MapToSomeOrNoneAsync<T, TResult>(this Option<T> data, AsyncFunc<T, TResult?> projection, CancellationToken ct)
		where TResult : struct
		=> data.Type switch
		{
			OptionType.Undefined => Option<TResult>.Undefined(),
			OptionType.None => Option<TResult>.None(),
			OptionType.Some when await projection(data.SomeOrDefault()!, ct).ConfigureAwait(false) is TResult result => result,
			OptionType.Some => Option<TResult>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Option<T> SomeOrNone<T>(Option<T?> data)
		=> data.Type switch
		{
			OptionType.Undefined => Option<T>.Undefined(),
			OptionType.None => Option<T>.None(),
			OptionType.Some when data.SomeOrDefault() is T value => value,
			OptionType.Some => Option<T>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Option<T> SomeOrNone<T>(Option<T?> data)
		where T : struct
		=> data.Type switch
		{
			OptionType.Undefined => Option<T>.Undefined(),
			OptionType.None => Option<T>.None(),
			OptionType.Some when data.SomeOrDefault() is T value => value,
			OptionType.Some => Option<T>.None(),
			_ => throw new InvalidOperationException("Unknown option type"),
		};
}
