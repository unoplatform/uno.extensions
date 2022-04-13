using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Bindings;

internal sealed class Input<T> : IInput<T>
{
	private readonly IState<T> _state;

	public string PropertyName { get; }

	public Input(string propertyName, IState<T> state)
	{
		_state = state;
		PropertyName = propertyName;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
		=> _state.GetSource(context, ct);

	/// <inheritdoc />
	public ValueTask Update(Func<Message<T>, MessageBuilder<T>> updater, CancellationToken ct)
		=> _state.Update(msg => updater(msg).Set(BindableViewModelBase.BindingSource, this), ct);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _state.DisposeAsync();
}
