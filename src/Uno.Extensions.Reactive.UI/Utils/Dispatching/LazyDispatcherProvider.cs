using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Dispatching;

/// <summary>
/// An helper class to create a <see cref="DispatcherHelper.FindDispatcher"/> with a callback
/// to get notified when the first dispatcher is being resolved.
/// </summary>
internal sealed class LazyDispatcherProvider
{
	private Action? _onFirstResolved;
	private readonly DispatcherHelper.FindDispatcher _dispatcherProvider;

	public LazyDispatcherProvider(Action onFirstResolved, DispatcherHelper.FindDispatcher? dispatcherProvider = null)
	{
		_onFirstResolved = onFirstResolved;
		_dispatcherProvider = dispatcherProvider ?? DispatcherHelper.GetForCurrentThread;
	}

	/// <summary>
	/// Try to run the callback if not yet ran and **if current thread is a UI thread**.
	/// </summary>
	public void TryRunCallback()
	{
		FindDispatcher();
	}

	/// <summary>
	/// Force to run the callback if not ran yet, **no matter if the current thread is a UI thread or not**.
	/// </summary>
	public void RunCallback()
	{
		if (Interlocked.Exchange(ref _onFirstResolved, null) is { } onFirstResolved)
		{
			onFirstResolved();
		}
	}

	public IDispatcherInternal? FindDispatcher()
	{
		if (_dispatcherProvider() is { } dispatcher)
		{
			RunCallback();

			return dispatcher;
		}
		else
		{
			return null;
		}
	}
}
