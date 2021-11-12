using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

public interface IState<T> : IFeed<T>, IAsyncDisposable
{
	ValueTask Update(Func<Message<T>, MessageBuilder<T>> updater, CancellationToken ct);
}
