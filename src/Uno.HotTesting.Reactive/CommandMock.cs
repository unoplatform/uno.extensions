using System;
using System.ComponentModel;
using Uno.Extensions.Reactive;

namespace Uno.HotTesting.Reactive;

/// <summary>
/// Typed vocabulary to build mocked commands (spec 013). All produce a strongly-typed
/// <see cref="IAsyncCommand"/> suitable for a <c>{Model}Mock</c> command override.
/// </summary>
public static class CommandMock
{
	/// <summary>An idle, executable no-op command.</summary>
	public static IAsyncCommand Idle() => new MockAsyncCommand(canExecute: true);

	/// <summary>A command that cannot be executed.</summary>
	public static IAsyncCommand Disabled() => new MockAsyncCommand(canExecute: false);

	/// <summary>A command pinned to the executing state.</summary>
	public static IAsyncCommand Executing() => new MockAsyncCommand(canExecute: true) { IsExecuting = true };

	/// <summary>An executable command invoking <paramref name="onExecute"/>.</summary>
	public static IAsyncCommand Callback(Action<object?> onExecute, bool canExecute = true)
		=> new MockAsyncCommand(canExecute) { OnExecute = onExecute ?? throw new ArgumentNullException(nameof(onExecute)) };

	private sealed class MockAsyncCommand : IAsyncCommand
	{
		private readonly bool _canExecute;

		public MockAsyncCommand(bool canExecute) => _canExecute = canExecute;

		public Action<object?>? OnExecute { get; init; }

		public bool IsExecuting { get; init; }

		public event EventHandler? CanExecuteChanged;
		public event EventHandler? IsExecutingChanged;
		public event PropertyChangedEventHandler? PropertyChanged;

		public bool CanExecute(object? parameter) => _canExecute;

		public void Execute(object? parameter) => OnExecute?.Invoke(parameter);

		// Referenced to avoid unused-event warnings; mock commands do not raise them.
		private void Touch()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			IsExecutingChanged?.Invoke(this, EventArgs.Empty);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExecuting)));
		}
	}
}
