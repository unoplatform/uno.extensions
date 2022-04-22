using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions;

public static class OptionExtensions
{
	public static async ValueTask<Option<TResult>> MapAsync<T, TResult>(this Option<T> option, AsyncFunc<T?, TResult?> projection, CancellationToken ct)
	=> option.IsSome(out var value) ? await projection(value, ct)
		: option.IsNone() ? Option<TResult>.None()
		: Option<TResult>.Undefined();

	public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> projection)
		=> option.IsSome(out var value) ? Option.Some(projection(value!))
			: option.IsNone() ? Option<TResult>.None()
			: Option<TResult>.Undefined();
}
