using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Testing;

public interface IFeedRecorder<T> : IReadOnlyList<Message<T>>, IReadOnlyCollection<Message<T>>
{
	// Warning: This interface could be used while creating the Feed of a FeedRecorder<TFeed, TValue>

	string Name { get; }

	ValueTask WaitForMessages(int count, CancellationToken ct, int timeout = 1000);
}
