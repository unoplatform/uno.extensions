using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Uno.Extensions.Reactive.Dispatching;

/// <summary>
/// Provides dispatcher-local storage of data
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
internal sealed class DispatcherLocal<T>
{
	private const string _cannotCreateForBackgroundThread = "This value cannot be created for a background thread";
	private const string _cannotCreateFromAnotherThread = "The value is not present and cannot be created from another thread";

	private readonly object _writeGate = new();
	private readonly bool _allowBackgroundValue;
	private readonly bool _allowCreationFromAnotherThread;
	private readonly Func<IDispatcher?, T> _factory;
	private readonly FindDispatcher _schedulersProvider;

	private _Value? _backgroundValue;
	private _Value? _mainUiValue; // As usually apps have only 1 UI thread, we try to avoid dictionary lookup if unnecessary.
	private ImmutableDictionary<IDispatcher, _Value>? _otherUiValues;

	/// <summary>
	/// Creates a new instance of a dispatcher local value
	/// </summary>
	/// <param name="factory">The optional factory use to create the value for all threads</param>
	/// <param name="schedulersProvider">The scheduler provider to use to determine the current thread. If none set, fallback to the default <see cref="DispatcherHelper.GetForCurrentThread"/>.</param>
	/// <param name="allowBackgroundValue">Indicates if a <typeparamref name="T"/> can be created for background threads</param>
	/// <param name="allowCreationFromAnotherThread">
	/// Determines if when using the <see cref="GetValue"/> or <see cref="TryGetValue"/> a value can be create for the given scheduler even if it's not the current
	/// </param>
	public DispatcherLocal(
		Func<T> factory,
		FindDispatcher? schedulersProvider = null,
		bool allowBackgroundValue = true,
		bool allowCreationFromAnotherThread = false)
	{
		_factory = _ => factory();
		_schedulersProvider = schedulersProvider ?? DispatcherHelper.GetForCurrentThread;
		_allowBackgroundValue = allowBackgroundValue;
		_allowCreationFromAnotherThread = allowCreationFromAnotherThread;
	}

	/// <summary>
	/// Creates a new instance of a dispatcher local value
	/// </summary>
	/// <param name="factory">The optional factory use to create the value for all threads</param>
	/// <param name="schedulersProvider">The scheduler provider to use to determine the current thread. If none set, fallback to the default <see cref="DispatcherHelper.GetForCurrentThread"/>.</param>
	/// <param name="allowBackgroundValue">Indicates if a <typeparamref name="T"/> can be created for background threads</param>
	/// <param name="allowCreationFromAnotherThread">
	/// Determines if when using the <see cref="GetValue"/> or <see cref="TryGetValue"/> a value can be create for the given scheduler even if it's not the current
	/// </param>
	public DispatcherLocal(
		Func<IDispatcher?, T> factory,
		FindDispatcher? schedulersProvider = null,
		bool allowBackgroundValue = true,
		bool allowCreationFromAnotherThread = false)
	{
		_factory = factory;
		_schedulersProvider = schedulersProvider ?? DispatcherHelper.GetForCurrentThread;
		_allowBackgroundValue = allowBackgroundValue;
		_allowCreationFromAnotherThread = allowCreationFromAnotherThread;
	}

	/// <summary>
	/// Gets the current value for the current thread
	/// </summary>
	public T Value
	{
		get
		{
			var current = _schedulersProvider();
			var (hasValue, value, error) = GetValueCore(current, current);

			if (hasValue)
			{
				return value!;
			}
			else
			{
				throw new InvalidOperationException(error);
			}
		}
		set => SetValueCore(_schedulersProvider(), value);
	}

	/// <summary>
	/// Gets value for the given scheduler.
	/// </summary>
	public T GetValue(IDispatcher scheduler)
	{
		var (hasValue, value, error) = GetValueCore(
			owner: scheduler,
			current: _schedulersProvider());

		if (hasValue)
		{
			return value!;
		}
		else
		{
			throw new InvalidOperationException(error);
		}
	}

	/// <summary>
	/// Try to gets value for the given scheduler.
	/// </summary>
	/// <remarks>
	/// If the value is not present for the given scheduler, and if the creation was allowed from another thread, then the value will be created.
	/// </remarks>
	public bool TryGetValue(IDispatcher scheduler, out T? value)
	{
		(var hasValue, value, _) = GetValueCore(
			owner: scheduler,
			current: _schedulersProvider());

		return hasValue;
	}

	private (bool hasValue, T? value, string? errorMessage) GetValueCore(IDispatcher? owner, IDispatcher? current)
	{
		var hasWriteAccess = false;
		try
		{
			if (owner is not null)
			{
				// The value is missing, we need to create a new one
				var createdValue = default(_Value);
				while (true)
				{
					if (_mainUiValue is null)
					{
						if (!_allowCreationFromAnotherThread && owner != current)
						{
							return (false, default, _cannotCreateFromAnotherThread);
						}

						if (!hasWriteAccess)
						{
							Monitor.Enter(_writeGate, ref hasWriteAccess);
						}

						createdValue ??= new _Value(owner, _factory(owner));
						if (Interlocked.CompareExchange(ref _mainUiValue, createdValue, null) is not null)
						{
							// We failed to assign the _mainUiValue, which indicates a concurrency conflict, retry whole process!
							continue;
						}

						return (true, createdValue.Value, null);
					}
					else if (_mainUiValue.Scheduler == owner)
					{
						return (true, _mainUiValue.Value, null);
					}

					var currentUiValues = _otherUiValues;
					if (currentUiValues is null || !currentUiValues.TryGetValue(owner, out var value))
					{
						if (!_allowCreationFromAnotherThread && owner != current)
						{
							return (false, default, _cannotCreateFromAnotherThread);
						}

						if (!hasWriteAccess)
						{
							Monitor.Enter(_writeGate, ref hasWriteAccess);
						}

						createdValue ??= new _Value(owner, _factory(owner));
						var updatedUiValues = (currentUiValues ?? ImmutableDictionary<IDispatcher, _Value>.Empty).Add(owner, createdValue);
						if (Interlocked.CompareExchange(ref _otherUiValues, updatedUiValues, currentUiValues) != currentUiValues)
						{
							// We failed to update the _uiValues, which indicates a concurrency conflict, retry whole process!
							continue;
						}

						return (true, createdValue.Value, null);
					}
					else
					{
						return (true, value.Value, null);
					}
				}
			}
			else if (_allowBackgroundValue)
			{
				if (_backgroundValue is null)
				{
					if (!_allowCreationFromAnotherThread && current is not null)
					{
						return (false, default, _cannotCreateFromAnotherThread);
					}

					if (!hasWriteAccess)
					{
						Monitor.Enter(_writeGate, ref hasWriteAccess);
					}

					Interlocked.CompareExchange(ref _backgroundValue, new _Value(null, _factory(owner)), null);
				}

				return (true, _backgroundValue.Value, null);
			}
			else
			{
				return (false, default, _cannotCreateForBackgroundThread);
			}
		}
		finally
		{
			if (hasWriteAccess)
			{
				Monitor.Exit(_writeGate);
			}
		}
	}

	private void SetValueCore(IDispatcher? scheduler, T rawValue)
	{
		lock (_writeGate)
		{
			if (scheduler is not null)
			{
				var value = new _Value(scheduler, rawValue);

				if (Interlocked.CompareExchange(ref _mainUiValue, value, null) is null)
				{
					return;
				}
				else if (_mainUiValue.Scheduler == scheduler)
				{
					_mainUiValue = value;
					return;
				}
				else
				{
					ImmutableInterlocked.Update(ref _otherUiValues, SetItem, value);

					static ImmutableDictionary<IDispatcher, _Value> SetItem(ImmutableDictionary<IDispatcher, _Value>? current, _Value v2)
						=> (current ?? ImmutableDictionary<IDispatcher, _Value>.Empty).SetItem(v2.Scheduler!, v2);
				}
			}
			else if (_allowBackgroundValue)
			{
				_backgroundValue = new _Value(null, rawValue);
			}
			else
			{
				throw new InvalidOperationException(_cannotCreateForBackgroundThread);
			} 
		}
	}

	/// <summary>
	/// Gets value for all dispatcher, and optionally for the background thread.
	/// WARNING: This method is only PARTIALLY thread-safe (no exception if value is created while enumerating, but newly created values won't be enumerated).
	/// </summary>
	public IEnumerable<(IDispatcher? scheduler, T value)> GetValues(bool includeBackground = true)
	{
		if (_mainUiValue is not null)
		{
			yield return (_mainUiValue.Scheduler, _mainUiValue.Value);

			if (_otherUiValues is { } otherUiValues)
			{
				foreach (var ui in otherUiValues)
				{
					yield return (ui.Key, ui.Value.Value);
				}
			}
		}

		if (includeBackground && _backgroundValue is not null)
		{
			yield return (null, _backgroundValue.Value);
		}
	}

	/// <summary>
	/// Executes an action for the value each dispatcher, and optionally for the background thread.
	/// WARNING: This method is thread-safe, but the action is executed in a lock, so it should be fast.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ForEachValue(Action<IDispatcher?, T> action, bool includeBackground = true)
		=> ForEachValue(static (a, d, v) => a(d, v), action, includeBackground);

	/// <summary>
	/// Executes an action for the value each dispatcher, and optionally for the background thread.
	/// WARNING: This method is thread-safe, but the action is executed in a lock, so it should be fast.
	/// </summary>
	public void ForEachValue<TState>(Action<TState, IDispatcher?, T> action, TState state, bool includeBackground = true)
	{
		lock (_writeGate)
		{
			if (_mainUiValue is not null)
			{
				action(state, _mainUiValue.Scheduler, _mainUiValue.Value);

				if (_otherUiValues is { } otherUiValues)
				{
					foreach (var ui in otherUiValues)
					{
						action(state, ui.Key, ui.Value.Value);
					}
				}
			}

			if (includeBackground && _backgroundValue is not null)
			{
				action(state, null, _backgroundValue.Value);
			}
		}
	}

	private class _Value
	{
		public _Value(IDispatcher? scheduler, T value)
		{
			Scheduler = scheduler;
			Value = value;
		}

		public IDispatcher? Scheduler { get; }

		public T Value { get; }
	}
}
