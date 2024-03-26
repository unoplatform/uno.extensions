using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

internal sealed class StateForEach<T> : IDisposable
	where T : notnull
{
	private readonly CancellationTokenSource _ct = new();
	private readonly AsyncAction<Option<T>> _action;
	private readonly string _name;
	private readonly Task _task; // Holds ref on the enumeration task. This is also accessed by reflection in tests!

	public StateForEach(ISignal<Message<T>> state, AsyncAction<Option<T>> action, string name = "-unnamed-")
	{
		if (state is not IStateImpl impl)
		{
			throw new InvalidOperationException("Execute is supported only on internal state implementation.");
		}

		_action = action;
		_name = name;

		_task = state
			.GetSource(impl.Context, _ct.Token)
			.Skip(1) // Ignore the original state
			.Where(msg => msg.Changes.Contains(MessageAxis.Data))
			.Select(msg => msg.Current.Data)
			.ForEachAwaitWithCancellationAsync(Execute, _ct.Token);

		// Make sure that we are not being collected if the caller ignores us
		ConditionalDisposable.Link(state, this);
	}

	private async Task Execute(Option<T> value, CancellationToken ct)
	{
		if (ct.IsCancellationRequested)
		{
			// If 'this' has been disposed, the ct will be cancelled,
			// but the ForEachAwaitWithCancellationAsync might still invoke us for one more value before completing.
			return;
		}

		try
		{
			await _action(value, ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error(error, $"Failed to execute callback for '{_name}' on state changed.");
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_ct.Cancel(throwOnFirstException: false);
		_ct.Dispose();
	}
}
