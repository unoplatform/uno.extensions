using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal class AsyncEnumerableSubject<T> : IAsyncEnumerable<T>
{
	private readonly ReplayMode _mode;
	private TaskCompletionSource<Node>? _head; // This is set only for replay
	private TaskCompletionSource<Node>? _current = new();

	public AsyncEnumerableSubject(bool replay = false)
	{
		if (replay)
		{
			_mode = ReplayMode.Enabled;
			_head = _current;
		}
	}

	internal AsyncEnumerableSubject(ReplayMode mode)
	{
		_mode = mode;
		if (mode != ReplayMode.Disabled)
		{
			_head = _current;
		}
	}

	public void SetNext(T item)
		=> MoveNext(true, item, error: null, mightHaveNext: true);

	public void Complete(T lastItem)
		=> MoveNext(true, lastItem, error: null, mightHaveNext: false);

	public void Complete()
		=> MoveNext(false, default, error: null, mightHaveNext: false);

	public void Fail(Exception error)
		=> MoveNext(false, default, error: error, mightHaveNext: false);

	public void TrySetNext(T item)
		=> MoveNext(true, item, error: null, mightHaveNext: true, throwOnError: false);

	public void TryComplete(T lastItem)
		=> MoveNext(true, lastItem, error: null, mightHaveNext: false, throwOnError: false);

	public void TryComplete()
		=> MoveNext(false, default, error: null, mightHaveNext: false, throwOnError: false);

	public void TryFail(Exception error)
		=> MoveNext(false, default, error: error, mightHaveNext: false, throwOnError: false);


	private void MoveNext(bool hasValue, T? value, Exception? error = null, bool mightHaveNext = true, bool throwOnError = true)
	{
		TaskCompletionSource<Node>? current;
		var next = mightHaveNext
			? new TaskCompletionSource<Node>() // Do not use AttachToParent to avoid leak of the current context which gives only the current value 
			: default;
		do
		{
			current = _current;
			if (current is null)
			{
				if (throwOnError)
				{
					throw new InvalidOperationException("Enumeration has already been completed.");
				}
				else
				{
					return;
				}
			}
		} while (Interlocked.CompareExchange(ref _current, next, current) != current);

		current.SetResult(new Node(hasValue, value, error, next));
	}

	/// <inheritdoc />
	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
	{
		var nextNode = _mode switch
		{
			ReplayMode.Disabled => _current,
			ReplayMode.Enabled => _head,
			ReplayMode.EnabledForFirstEnumeratorOnly => Interlocked.Exchange(ref _head, null) ?? _current,
			_ => throw new NotSupportedException($"Unknown replay mode '{_mode}'"),
		};

		while (nextNode is not null)
		{
			var current = await nextNode.Task.ConfigureAwait(false);
			if (current.HasValue)
			{
				yield return current.Value!;
				nextNode = current.Next;
			}
			else if (current.Error is not null)
			{
				ExceptionDispatchInfo.Capture(current.Error).Throw();
				yield break;
			}
			else
			{
				yield break;
			}
		}
	}

	private readonly struct Node
	{
		public readonly bool HasValue;
		public readonly T? Value;
		public readonly Exception? Error;
		public readonly TaskCompletionSource<Node>? Next;

		public Node(bool hasValue, T? value, Exception? error, TaskCompletionSource<Node>? next)
		{
			HasValue = hasValue;
			Value = value;
			Error = error;
			Next = next;
		}
	}
}
