using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal static class AsyncEnumeratorExtensions
{
	/// <summary>
	/// Moves to the next element, throwing if <paramref name="ct"/> was cancelled in the meantime.
	/// </summary>
	/// <remarks>
	/// The BCL's <see cref="IAsyncEnumerator{T}"/> takes its cancellation token at <c>GetAsyncEnumerator</c> time and
	/// exposes only a parameterless <c>MoveNextAsync</c>, so cancellation between two moves is not observed on its own.
	/// </remarks>
	public static ValueTask<bool> MoveNextAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		return enumerator.MoveNextAsync();
	}
}
