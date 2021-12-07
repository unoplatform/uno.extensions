using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Events;

internal class CoalescingDispatcherInvocationList<THandler, TArgs> : DispatcherInvocationList<THandler, TArgs>
	where TArgs : class
{
	private TArgs? _pending;

	public CoalescingDispatcherInvocationList(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, DispatcherQueue dispatcher)
		: base(owner, raiseMethod, dispatcher)
	{
	}

	/// <inheritdoc />
	protected override void Enqueue(TArgs args)
		=> _pending = args;

	/// <inheritdoc />
	protected override void Dequeue()
		=> RaiseCore(Interlocked.Exchange(ref _pending, default));
}
