using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct FeedMethodBuilder<T>
{
	[ThreadStatic]
	private static FeedMethodBuilder<T> _current;

	private readonly DummyFeed<T>? _task;

	private	FeedMethodBuilder(DummyFeed<T> task) => _task = task;

	public IFeed<T> Task => _task!;

	public static FeedMethodBuilder<T> Create()
		=> new(new());

	public void Start<TStateMachine>(ref TStateMachine stateMachine)
		where TStateMachine : IAsyncStateMachine
	{
		//_stateMachineFactory ??= () => new TStateMachine();
		var previous = _current;
		_current = this; // No needs for try/finally, MoveNext cannot throw
		stateMachine.MoveNext();
		_current = previous;
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine) { }

	public void SetException(Exception exception)
		=> _task!.SetException(exception);

	public void SetResult(T result)
		=> _task!.SetResult(result);


	public void AwaitOnCompleted<TAwaiter, TStateMachine>(
		ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : INotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		var sm = stateMachine;
		var that = this;
		awaiter.OnCompleted(() =>
		{
			var previous = _current;
			_current = that; // No needs for try/finally, MoveNext cannot throw
			sm.MoveNext();
			_current = previous;
		});
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
		ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : ICriticalNotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		var sm = stateMachine;
		var that = this;
		awaiter.OnCompleted(() =>
		{
			var previous = _current;
			_current = that; // No needs for try/finally, MoveNext cannot throw
			sm.MoveNext();
			_current = previous;
		});
	}
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class DummyFeed<T> : TaskCompletionSource<T?>, IFeed<T>
{
	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
		=> throw new InvalidOperationException("You must enable code interceptor to use the imperative feed syntax.");

	public Task<T?> GetResult(CancellationToken ct) => Task;
}
