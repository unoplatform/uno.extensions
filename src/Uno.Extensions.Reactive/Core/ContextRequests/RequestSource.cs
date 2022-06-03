using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class RequestSource : IRequestSource
{
	private readonly AsyncEnumerableSubject<IContextRequest> _subject = new();

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
