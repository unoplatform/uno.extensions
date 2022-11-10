using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Dispatching;

/// <summary>
/// A dispatcher queue instance that will execute tasks serially on the current thread, or null if no such queue exists.
/// </summary>
/// <returns>The dispatcher associated to the current thread if the thread is a UI thread.</returns>
public delegate IDispatcher? FindDispatcher();

/// <summary>
/// Helper class to allow resolution of the <see cref="IDispatcher"/> for the reactive framework.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class DispatcherHelper
{
	/// <summary>
	/// Defines how the reactive framework resolves the dispatcher of UI thread.
	/// </summary>
	public static FindDispatcher GetForCurrentThread = NotConfigured;

	/// <summary>
	/// Gets a value indicating whether the dispatcher has access to the current thread.
	/// </summary>
	public static bool HasThreadAccess => GetForCurrentThread()?.HasThreadAccess ?? false;

	/// <summary>
	/// Get the dispatcher of the current UI thread, or throw if invoked from a background thread.
	/// </summary>
	/// <exception cref="InvalidOperationException">If the method has been invoked from a background thread.</exception>
	/// <returns>The dispatcher of the current UI thread.</returns>
	public static IDispatcher GetDispatcher()
		=> GetDispatcher(null);

	/// <summary>
	/// Get the dispatcher of the current UI thread given an optional custom dispatcher, or throw if invoked from a background thread.
	/// </summary>
	/// <exception cref="InvalidOperationException">The given dispatcher is not set and the method has been invoked from a background thread.</exception>
	/// <returns>The given dispatcher if set, or the dispatcher of the current UI thread.</returns>
	public static IDispatcher GetDispatcher(IDispatcher? given)
		=> given
			?? GetForCurrentThread()
			?? throw new InvalidOperationException("Failed to get dispatcher to use. Either explicitly provide the dispatcher to use, either make sure to invoke this on the UI thread.");

	private static IDispatcher NotConfigured()
		=> throw new InvalidOperationException(
			"The API you are using requires access to the dispatcher but it has not been setup for this platform. "
			+ "You need to add a reference to Uno.Extensions.Reactive.UI or Uno.Extensions.Reactive.WinUI package in you application head, "
			+ "or Uno.Extensions.Reactive.Testing if your running a test project. "
			+ "Alternatively you have to manually setup the dispatcher provider by setting the Uno.Extensions.Reactive.Dispatching.DispatcherHelper.GetForCurrentThread (not recommended).");
}
