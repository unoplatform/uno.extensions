using System;
using System.ComponentModel;

namespace Uno.Extensions.Reactive.Mocks;

/// <summary>
/// Provides factories of <see cref="IAsyncCommand"/> pinned to a fixed state, for previews and UI tests of visual states.
/// </summary>
public static class MockCommand
{
	/// <summary>
	/// Gets a command that can be executed but does nothing.
	/// </summary>
	/// <returns>A command pinned in the idle state.</returns>
	public static IAsyncCommand Idle()
		=> new MockAsyncCommand(canExecute: true, isExecuting: false);

	/// <summary>
	/// Gets a command that cannot be executed.
	/// </summary>
	/// <returns>A command pinned in the disabled state.</returns>
	public static IAsyncCommand Disabled()
		=> new MockAsyncCommand(canExecute: false, isExecuting: false);

	/// <summary>
	/// Gets a command pinned in the executing state, indefinitely.
	/// </summary>
	/// <returns>A command pinned in the executing state.</returns>
	public static IAsyncCommand Executing()
		=> new MockAsyncCommand(canExecute: false, isExecuting: true);

	/// <summary>
	/// Gets a command that invokes the given callback when executed.
	/// </summary>
	/// <param name="onExecute">The callback to invoke on execution. Must be synchronous: exceptions propagate raw to the invoker, and an <c>async</c> lambda would be an unobservable async-void.</param>
	/// <param name="canExecute">Determines if the command can be executed.</param>
	/// <returns>A command that invokes the given callback when executed.</returns>
	public static IAsyncCommand Callback(Action<object?> onExecute, bool canExecute = true)
	{
		onExecute = onExecute ?? throw new ArgumentNullException(nameof(onExecute));

		return new MockAsyncCommand(canExecute, isExecuting: false, onExecute);
	}
}

/// <summary>
/// An <see cref="IAsyncCommand"/> pinned to a fixed state — the state never changes, so change events are never raised.
/// </summary>
internal sealed class MockAsyncCommand : IAsyncCommand
{
	private readonly bool _canExecute;
	private readonly Action<object?>? _onExecute;

	public MockAsyncCommand(bool canExecute, bool isExecuting, Action<object?>? onExecute = null)
	{
		_canExecute = canExecute;
		IsExecuting = isExecuting;
		_onExecute = onExecute;
	}

	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged { add { } remove { } }

	/// <inheritdoc />
	public event EventHandler? IsExecutingChanged { add { } remove { } }

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

	/// <inheritdoc />
	public bool IsExecuting { get; }

	/// <inheritdoc />
	public bool CanExecute(object? parameter)
		=> _canExecute;

	/// <inheritdoc />
	public void Execute(object? parameter)
	{
		// Like the real AsyncCommand, a programmatic Execute on a non-executable command is a no-op.
		if (_canExecute)
		{
			_onExecute?.Invoke(parameter);
		}
	}
}
