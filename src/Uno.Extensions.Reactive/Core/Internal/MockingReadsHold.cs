using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Mocking (spec 013): holds the first read of the subscriptions created while a mock is applied to a model under construction.
/// </summary>
/// <remarks>
/// Awaiting it resumes inside <see cref="Release"/>, on the releasing thread, whatever its synchronization context:
/// a task would move the reads of a UI thread to the thread pool, and the view would render before they land.
/// </remarks>
internal sealed class MockingReadsHold : INotifyCompletion
{
	private readonly object _gate = new();
	private List<Action>? _held = new();
	private bool _isReleased;

	public bool IsCompleted
	{
		get
		{
			lock (_gate)
			{
				return _isReleased;
			}
		}
	}

	public MockingReadsHold GetAwaiter() => this;

	public void GetResult()
	{
	}

	public void OnCompleted(Action continuation)
	{
		lock (_gate)
		{
			if (_held is { } held)
			{
				held.Add(continuation);
				return;
			}
		}

		continuation();
	}

	/// <summary>
	/// Resumes every held read. One that throws does not keep the others from resuming.
	/// </summary>
	public void Release()
	{
		List<Action>? held;
		lock (_gate)
		{
			_isReleased = true;
			held = _held;
			_held = null;
		}

		if (held is null)
		{
			return;
		}

		List<Exception>? errors = null;
		foreach (var continuation in held)
		{
			try
			{
				continuation();
			}
			catch (Exception error)
			{
				(errors ??= new()).Add(error);
			}
		}

		if (errors is not null)
		{
			throw new AggregateException(errors);
		}
	}
}
