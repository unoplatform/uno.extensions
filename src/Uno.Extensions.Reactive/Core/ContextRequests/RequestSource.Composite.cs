using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class CompositeRequestSource : IRequestSource
{
	private readonly AsyncEnumerableSubject<IContextRequest> _subject = new();

	/// <summary>
	/// Adds a new source to this composite source.
	/// </summary>
	/// <param name="other">The source to add.</param>
	/// <param name="ct">A cancellation token that can be used to remove the given source.</param>
	public void Add(IRequestSource other, CancellationToken ct)
		=> other.ForEachAsync(
			req =>
			{
				// There is kind of a bug in the ForEach, which wait for the **next** item before checking the CT
				if (ct is not { IsCancellationRequested: true })
				{
					_subject.SetNext(req);
				}
			},
			ct);

	/// <inheritdoc />
	public IAsyncEnumerator<IContextRequest> GetAsyncEnumerator(CancellationToken cancellationToken)
		=> _subject.GetAsyncEnumerator(cancellationToken);

	/// <inheritdoc />
	public void Send(IContextRequest request)
		=> _subject.SetNext(request);

	/// <inheritdoc />
	public void Dispose()
		=> _subject.TryComplete();
}
