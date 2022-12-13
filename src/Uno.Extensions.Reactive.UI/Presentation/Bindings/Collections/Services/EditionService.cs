using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Threading;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

internal class EditionService : IEditionService, IDisposable
{
	private readonly FastAsyncLock _gate = new();
	private readonly CancellationTokenSource _ct = new();
	private readonly AsyncAction<Func<IDifferentialCollectionNode, IDifferentialCollectionNode>> _editFromView;

	private ImmutableQueue<Func<IDifferentialCollectionNode, IDifferentialCollectionNode>> _editions = ImmutableQueue<Func<IDifferentialCollectionNode, IDifferentialCollectionNode>>.Empty;

	public EditionService(AsyncAction<Func<IDifferentialCollectionNode, IDifferentialCollectionNode>> editFromView)
	{
		_editFromView = editFromView;
	}

	/// <inheritdoc />
	public void Update(Func<IDifferentialCollectionNode, IDifferentialCollectionNode> edition)
	{
		ImmutableInterlocked.Enqueue(ref _editions, edition);
		Task.Run(Dequeue, _ct.Token);
	}

	private async Task Dequeue()
	{
		// Make sure we are not running multiple dequeue in //
		using var _ = await _gate.LockAsync(_ct.Token);

		var editions = Interlocked.Exchange(ref _editions, ImmutableQueue<Func<IDifferentialCollectionNode, IDifferentialCollectionNode>>.Empty);
		foreach (var edition in editions)
		{
			await _editFromView(edition, _ct.Token);
		}
	}

	/// <inheritdoc />
	public void Dispose()
		=> _ct.Cancel();
}
