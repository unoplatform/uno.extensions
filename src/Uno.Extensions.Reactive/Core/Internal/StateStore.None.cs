using System;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Core;

internal class NoneStateStore : IStateStore
{
	/// <inheritdoc />
	public TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
		where TSource : class
		where TState : IStateImpl, IAsyncDisposable
		=> throw new InvalidOperationException("Cannot create a state on SourceContext.None. " + SourceContext.NoneContextErrorDesc);

	/// <inheritdoc />
	public TState CreateState<T, TState>(Option<T> initialValue, Func<SourceContext, Option<T>, TState> factory)
		where TState : IStateImpl, IAsyncDisposable
		=> throw new InvalidOperationException("Cannot create a state on SourceContext.None. " + SourceContext.NoneContextErrorDesc);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
	}
}
