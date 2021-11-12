using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class MergeAsyncEnumerable<T> : IAsyncEnumerable<T>
{
	private readonly List<IAsyncEnumerable<T>> _sources;

	public MergeAsyncEnumerable(IEnumerable<IAsyncEnumerable<T>> sources)
	{
		_sources = sources.Where(source => source is not null).ToList();
	}

	/// <inheritdoc />
	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
	{
		var tasks = _sources
			.Select(source => source.GetAsyncEnumerator(ct))
			.ToDictionary(e => e, MoveNext);

		async Task<(IAsyncEnumerator<T> enumerator, bool movedNext)> MoveNext(IAsyncEnumerator<T> enumerator)
			=> (enumerator, await enumerator.MoveNextAsync(ct).ConfigureAwait(false));

		while (!ct.IsCancellationRequested && tasks.Any())
		{
			var (enumerator, hasCurrent) = await (await Task.WhenAny(tasks.Values).ConfigureAwait(false)).ConfigureAwait(false);
			if (hasCurrent)
			{
				yield return enumerator.Current;
				tasks[enumerator] = MoveNext(enumerator);
			}
			else
			{
				tasks.Remove(enumerator);
			}
		}
	}
}
