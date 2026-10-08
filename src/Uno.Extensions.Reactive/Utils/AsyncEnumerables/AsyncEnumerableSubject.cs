using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

/// <summary>
/// A push-pull adapter for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the value produced.</typeparam>
public class AsyncEnumerableSubject<T> : IAsyncEnumerable<T>
{
	private readonly AsyncEnumerableReplayMode _mode;
	private TaskCompletionSource<Node>? _head; // This is set only for replay
	private TaskCompletionSource<Node>? _current = new();

	internal AsyncEnumerableSubject(bool replay = false)
	{
		if (replay)
		{
			_mode = AsyncEnumerableReplayMode.Enabled;
			_head = _current;
		}
	}

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="mode">Specify the replay mode used for this push-pull adapter.</param>
	public AsyncEnumerableSubject(AsyncEnumerableReplayMode mode)
	{
		_mode = mode;
		if (mode != AsyncEnumerableReplayMode.Disabled)
		{
			_head = _current;
		}
	}

	/// <summary>
	/// Appends a new item into this async enumerable sequence.
	/// </summary>
	/// <param name="item">The next item.</param>
	public void SetNext(T item)
		=> MoveNext(true, item, error: null, mightHaveNext: true);

	/// <summary>
	/// Appends a new  value and completes this async enumerable sequence.
	/// </summary>
	/// <param name="lastItem">The last item to append to this sequence.</param>
	public void Complete(T lastItem)
		=> MoveNext(true, lastItem, error: null, mightHaveNext: false);

	/// <summary>
	/// Completes this async enumerable sequence.
	/// </summary>
	public void Complete()
		=> MoveNext(false, default, error: null, mightHaveNext: false);

	/// <summary>
	/// Completes this async enumerable sequence by throwing an exception.
	/// </summary>
	/// <param name="error">The exception to throw.</param>
	public void Fail(Exception error)
		=> MoveNext(false, default, error: error, mightHaveNext: false);

	/// <summary>
	/// Attempts to append a new item into this async enumerable sequence if not already completed.
	/// </summary>
	/// <param name="item">The next item.</param>
	public void TrySetNext(T item)
		=> MoveNext(true, item, error: null, mightHaveNext: true, throwOnError: false);

	/// <summary>
	/// Attempts to append a new  value and completes this async enumerable sequence if not already completed.
	/// </summary>
	/// <param name="lastItem">The last item to append to this sequence.</param>
	public void TryComplete(T lastItem)
		=> MoveNext(true, lastItem, error: null, mightHaveNext: false, throwOnError: false);

	/// <summary>
	/// Attempts to complete this async enumerable sequence if not already completed.
	/// </summary>
	public void TryComplete()
		=> MoveNext(false, default, error: null, mightHaveNext: false, throwOnError: false);

	/// <summary>
	/// Attempts to complete this async enumerable sequence by throwing an exception.
	/// </summary>
	/// <param name="error">The exception to throw.</param>
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
			AsyncEnumerableReplayMode.Disabled => _current,
			AsyncEnumerableReplayMode.Enabled => _head,
			AsyncEnumerableReplayMode.EnabledForFirstEnumeratorOnly => Interlocked.Exchange(ref _head, null) ?? _current,
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
