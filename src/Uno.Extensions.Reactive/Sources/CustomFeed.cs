using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class CustomFeed<T> : IFeed<T>
{
	private readonly Func<CancellationToken, IAsyncEnumerable<Message<T>>> _sourceProvider;

	public CustomFeed(Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
	{
		_sourceProvider = sourceProvider;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		using var _ = context.AsCurrent();

		var isFirstMessage = true;
		var previous = Message<T>.Initial;

		IAsyncEnumerator<Message<T>>? enumerator = default;
		Message<T>? current = default;
		Exception? error = default;
		bool hasCurrent;
		do
		{
			try
			{
				enumerator ??= _sourceProvider(ct).GetAsyncEnumerator(ct);
				hasCurrent = await enumerator.MoveNextAsync().ConfigureAwait(false);
				current = hasCurrent ? enumerator.Current! : default;

				if (hasCurrent && previous.Current != current!.Previous)
				{
					throw new InvalidOperationException("The provided message was not built from the current.");
				}
			}
			catch (Exception e)
			{
				error = e;
				hasCurrent = false; // Safety only
			}

			if (error is not null)
			{
				yield return previous.With().Error(error).IsTransient(false);
				yield break;
			}

			if (hasCurrent)
			{
				yield return previous = current!;
				isFirstMessage = false;
			}
		} while (hasCurrent);

		if (isFirstMessage)
		{
			// Make sure to at least play en empty message
			yield return previous;
		}
	}
}
