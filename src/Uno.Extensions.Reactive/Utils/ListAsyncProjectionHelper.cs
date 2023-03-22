using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Utils;

/// <summary>
/// An helper class that asynchronously project each item of a source collection into another type.
/// </summary>
/// <typeparam name="TSource">The type of the source items.</typeparam>
/// <typeparam name="TResult">The type of the target items.</typeparam>
public sealed class ListAsyncProjectionHelper<TSource, TResult> : IDisposable
{
	private readonly object _stateGate = new();
	private readonly object _updateGate = new(); // This is only for user which usually doesn't expect to be invoked in //
	private readonly Func<TSource, TResult> _syncProjection;
	private readonly AsyncFunc<TSource, TResult, TResult> _asyncProjection;
	private readonly CollectionAnalyzer<TSource> _analyzer;

	private ImmutableList<Value> _values = ImmutableList<Value>.Empty;
	private IImmutableList<TSource> _currentSourceItems = ImmutableList<TSource>.Empty;
	private DifferentialImmutableList<TResult> _result = DifferentialImmutableList<TResult>.Empty;
	private IImmutableList<Exception> _exceptions = ImmutableList<Exception>.Empty;
	private int _pending;
	private bool _isDisposed;

	/// <summary>
	/// An event raised when either <see cref="CurrentResult"/> of <see cref="CurrentError"/> has been updated.
	/// </summary>
	public event EventHandler? Updated;

	/// <summary>
	/// Gets the number of pending async projection.
	/// </summary>
	public long CurrentPending { get; private set; }

	/// <summary>
	/// Gets the result items.
	/// </summary>
	public IImmutableList<TResult> CurrentResult { get; private set; } = DifferentialImmutableList<TResult>.Empty;

	/// <summary>
	/// Gets an aggregate exception of error raised while asynchronously loading result, if any.
	/// </summary>
	/// <remarks>This will **NOT** contains exception raised by the sync projection.</remarks>
	public AggregateException? CurrentError { get; private set; }

	/// <summary>
	/// Creates a new instance
	/// </summary>
	/// <param name="syncProjection">A synchronous projection that will be applied when a new item is added in the source collection.</param>
	/// <param name="asyncProjection">The asynchronous projection that will be performed after the item has been added in the <see cref="CurrentResult"/> collection.</param>
	/// <remarks>
	/// While exception thrown in <paramref name="asyncProjection"/> will be reported in the <see cref="CurrentError"/>,
	/// exceptions thrown by the <paramref name="syncProjection"/> will be thrown synchronously while <see cref="Update"/>.
	/// </remarks>
	public ListAsyncProjectionHelper(Func<TSource, TResult> syncProjection, AsyncFunc<TSource, TResult, TResult> asyncProjection)
	{
		_syncProjection = syncProjection;
		_asyncProjection = asyncProjection;
		_analyzer = ListFeed<TSource>.DefaultAnalyzer;
	}

	/// <summary>
	/// Updates teh source collection that is going to be asynchronously projected to a collection of <typeparamref name="TResult"/>.
	/// </summary>
	/// <param name="items">The source items</param>
	/// <param name="changes">An optional change set that indicates what has been modified in the <paramref name="items"/> compared to the previous collection.</param>
	public void Update(IImmutableList<TSource> items, IChangeSet? changes = default)
	{
		lock (_stateGate)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(ListAsyncProjectionHelper<TSource, TResult>));
			}

			if (changes is not CollectionChangeSet<TSource> collectionChanges)
			{
				collectionChanges = _analyzer.GetChanges(_currentSourceItems, items);
			}

			var visitor = new Visitor(this, _values, _result);

			collectionChanges.Visit(visitor);

			_currentSourceItems = items;
			(_values, _result) = visitor.Complete();
		}

		lock (_updateGate)
		{
			CurrentPending = _pending;
			CurrentResult = _result;

			Updated?.Invoke(this, EventArgs.Empty);
		}
	}

	private void ReportResult(Value value, TResult result)
	{
		lock (_stateGate)
		{
			if (_isDisposed)
			{
				return;
			}

			var index = _values.IndexOf(value);
			if (index >= 0)
			{
				_result = _result.ReplaceAt(index, result);
			}
			else
			{
				this.Log().Warn("Got result for a missing value.");
			}
		}

		lock (_updateGate)
		{
			CurrentPending = _pending;
			CurrentResult = _result;

			Updated?.Invoke(this, EventArgs.Empty);
		}
	}

	private void ReportException(Exception error)
	{
		lock (_stateGate)
		{
			if (_isDisposed)
			{
				return;
			}

			_exceptions = _exceptions.Add(error);
		}

		lock(_updateGate)
		{
			CurrentPending = _pending;
			CurrentError = new AggregateException(_exceptions);

			Updated?.Invoke(this, EventArgs.Empty);
		}
	}

	private void RemoveException(Exception error)
	{
		lock (_stateGate)
		{
			if (_isDisposed)
			{
				return;
			}

			_exceptions = _exceptions.Remove(error);
		}

		lock (_updateGate)
		{
			CurrentPending = _pending;
			CurrentError = _exceptions is { Count: 0 } ? null : new AggregateException(_exceptions);

			Updated?.Invoke(this, EventArgs.Empty);
		}
	}

	private class Visitor : CollectionChangeSetVisitorBase<TSource>
	{
		private readonly ListAsyncProjectionHelper<TSource, TResult> _owner;
		private readonly ImmutableList<Value>.Builder _values;
		private DifferentialImmutableList<TResult> _result;

		public Visitor(ListAsyncProjectionHelper<TSource, TResult> owner, ImmutableList<Value> values, DifferentialImmutableList<TResult> result)
		{
			_owner = owner;
			_values = values.ToBuilder();
			_result = result;
		}

		public (ImmutableList<Value> values, DifferentialImmutableList<TResult> result) Complete()
			=> (_values.ToImmutable(), _result);

		/// <inheritdoc />
		public override void Add(IReadOnlyList<TSource> items, int index)
		{
			if (items.Count is 0)
			{
				return;
			}

			var values = new Value[items.Count];
			for (var i = 0; i < items.Count; i++)
			{
				values[i] = new Value(_owner, items[i]);
			}

			_result = _result.InsertRange(index, values.Select(v => v.Result).ToImmutableList());
			_values.InsertRange(index, values);
		}

		/// <inheritdoc />
		public override void Same(IReadOnlyList<TSource> original, IReadOnlyList<TSource> updated, int index)
		{
			// Nothing to do
		}

		// /// <inheritdoc />
		// public void Replace(IReadOnlyList<TSource> original, IReadOnlyList<TSource> updated, int index)
		// => Use base implementation

		/// <inheritdoc />
		public override void Move(IReadOnlyList<TSource> items, int fromIndex, int toIndex)
		{
			if (items.Count is 0)
			{
				return;
			}

			_result = _result.MoveRange(fromIndex, toIndex, items.Count);
			var values = new Value[items.Count];
			for (var i = 0; i < items.Count; i++)
			{
				values[i] = _values[fromIndex];
				_values.RemoveAt(fromIndex);
			}
			_values.InsertRange(toIndex, values);
		}

		/// <inheritdoc />
		public override void Remove(IReadOnlyList<TSource> items, int index)
		{
			_result = _result.RemoveRange(index, items.Count);
			for (var i = 0; i < items.Count; i++)
			{
				_values[index].Dispose();
				_values.RemoveAt(index);
			}
		}

		/// <inheritdoc />
		public override void Reset(IReadOnlyList<TSource> oldItems, IReadOnlyList<TSource> newItems)
		{
			Debug.Assert(oldItems.Count == _values.Count);

			foreach (var value in _values)
			{
				value.Dispose();
			}

			_result = DifferentialImmutableList<TResult>.Empty;
			_values.Clear();

			Add(newItems, 0);
		}
	}

	private class Value : IDisposable
	{
		private readonly ListAsyncProjectionHelper<TSource, TResult> _owner;
		private readonly CancellationTokenSource _ct = new();

		private readonly TSource _source;
		private Exception? _error;

		public Value(ListAsyncProjectionHelper<TSource, TResult> owner, TSource source)
		{
			_owner = owner;
			_source = source;
			Result = owner._syncProjection(source);

			_ = StartAsyncProjection();
		}

		public TResult Result { get; private set; }

		private async Task StartAsyncProjection()
		{
			try
			{
				var asyncResult = _owner._asyncProjection.Invoke(_source, Result, _ct.Token);
				if (asyncResult.IsCompletedSuccessfully)
				{
					// Fast path that prevents too much Update if async projection is actually sync!
					// If faulted, let throw and _owner.ReportException as error in async projection are expected to be exposed in the CurrentError
					Result = asyncResult.Result;
					return;
				}

				try
				{
					Interlocked.Increment(ref _owner._pending);
					Result = await asyncResult.ConfigureAwait(false);
				}
				finally
				{
					Interlocked.Decrement(ref _owner._pending);
				}

				if (!_ct.IsCancellationRequested)
				{
					_owner.ReportResult(this, Result);
				}
			}
			catch (OperationCanceledException) when (_ct.IsCancellationRequested)
			{
			}
			catch (Exception error)
			{
				_error = error;
				if (!_ct.IsCancellationRequested)
				{
					_owner.ReportException(error);
				}
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_ct.Cancel();
			if (_error is {} error)
			{
				_owner.RemoveException(error);
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		lock (_stateGate)
		{
			Update(ImmutableList<TSource>.Empty, _analyzer.GetResetChange(_currentSourceItems, ImmutableList<TSource>.Empty));
			_isDisposed = true;
		}
	}
}
