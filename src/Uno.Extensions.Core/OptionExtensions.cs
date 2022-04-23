using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions;

/// <summary>
/// Set of helpers to manipulate <see cref="Option"/>.
/// </summary>
public static class OptionExtensions
{
	/// <summary>
	/// Asynchronously projects the value of an option to another value.
	/// </summary>
	/// <typeparam name="T">Type of the source value.</typeparam>
	/// <typeparam name="TResult">Type of the result value.</typeparam>
	/// <param name="option">The option to map.</param>
	/// <param name="projection">The projection delegate.</param>
	/// <param name="ct">A token to cancel the asynchronous operation.</param>
	/// <returns>An async operation to track the async projection.</returns>
	/// <remarks>This will invoke the <paramref name="projection"/> only if the <paramref name="option"/> is <see cref="OptionType.Some"/>.</remarks>
	public static async ValueTask<Option<TResult>> MapAsync<T, TResult>(this Option<T> option, AsyncFunc<T, TResult> projection, CancellationToken ct)
		=> option.IsSome(out var value) ? await projection(value, ct)
			: option.IsNone() ? Option<TResult>.None()
			: Option<TResult>.Undefined();

	/// <summary>
	/// Projects the value of an option to another value.
	/// </summary>
	/// <typeparam name="T">Type of the source value.</typeparam>
	/// <typeparam name="TResult">Type of the result value.</typeparam>
	/// <param name="option">The option to map.</param>
	/// <param name="projection">The projection delegate.</param>
	/// <returns>The result option of the projection.</returns>
	/// <remarks>This will invoke the <paramref name="projection"/> only if the <paramref name="option"/> is <see cref="OptionType.Some"/>.</remarks>
	public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> projection)
		=> option.IsSome(out var value) ? Option.Some(projection(value!))
			: option.IsNone() ? Option<TResult>.None()
			: Option<TResult>.Undefined();
}
