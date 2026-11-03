using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Bindings;

internal sealed class Input<T> : IInput<T>, IValidationTarget
{
	private readonly IState<T> _state;

	/// <inheritdoc />
	SourceContext IState.Context => _state.Context;

	/// <inheritdoc />
	IRequestSource IState.Requests => _state.Requests;

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
	public ValueTask UpdateMessageAsync(Action<MessageBuilder<T>> updater, CancellationToken ct)
		=> _state.UpdateMessageAsync(
			msg =>
			{
				updater(msg);
				msg.Set(BindableViewModelBase.BindingSource, this);
			},
			ct);

	/// <inheritdoc />
	/// <remarks>Forwarded to the wrapped state, so validation results published on the input are ordered with the ones of the state (the last validation started wins).</remarks>
	ValueTask IValidationTarget.PublishValidationAsync(IImmutableList<ValidationResult> results, long version, CancellationToken ct)
		=> ValidationHelper.GetPublisher(_state)(results, version, ct);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _state.DisposeAsync();
}
