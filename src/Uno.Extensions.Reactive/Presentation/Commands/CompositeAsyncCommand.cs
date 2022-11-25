using System;
using System.ComponentModel;
using System.Linq;
using Uno.Extensions.Reactive.Events;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// An <see cref="IAsyncCommand"/> that wraps a set of commands.
/// </summary>
/// <remarks>On <see cref="Execute"/>, this will apply execute **all** commands that report <see cref="CanExecute"/> true.</remarks>
public class CompositeAsyncCommand : IAsyncCommand
{
	private static readonly PropertyChangedEventArgs _isExecutingChangedArgs = new(nameof(IsExecuting));

	private readonly IAsyncCommand[] _commands;

	private readonly EventManager<EventHandler, EventArgs> _canExecuteChanged;
	private readonly EventManager<EventHandler, EventArgs> _isExecutingChanged;
	private readonly EventManager<PropertyChangedEventHandler, PropertyChangedEventArgs> _propertyChanged;

	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged
	{
		add => _canExecuteChanged.Add(value);
		remove => _canExecuteChanged.Remove(value);
	}

	/// <inheritdoc />
	public event EventHandler? IsExecutingChanged
	{
		add => _isExecutingChanged.Add(value);
		remove => _isExecutingChanged.Remove(value);
	}

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged
	{
		add => _propertyChanged.Add(value);
		remove => _propertyChanged.Remove(value);
	}

	/// <summary>
	/// Creates a new instance of composite async command
	/// </summary>
	/// <param name="commands">The inner async commands.</param>
	public CompositeAsyncCommand(params IAsyncCommand[] commands)
	{
		_commands = commands;

		_canExecuteChanged = new(this, h => h.Invoke, isCoalescable: true);
		_isExecutingChanged = new(this, h => h.Invoke, isCoalescable: true);
		_propertyChanged = new(this, h => h.Invoke, isCoalescable: false);

		var weakThis = new WeakReference<CompositeAsyncCommand>(this);
		var weakCanExecuteChangedHandler = new EventHandler(OnCanExecuteChanged);
		var weakIsExecutingChangedHandler = new EventHandler(OnIsExecutingChanged);

		foreach (var command in commands)
		{
			command.CanExecuteChanged += weakCanExecuteChangedHandler;
			command.IsExecutingChanged += weakIsExecutingChangedHandler;
		}

		UpdateIsExecuting();

		void OnCanExecuteChanged(object? _, EventArgs args)
		{
			if (weakThis.TryGetTarget(out var that))
			{
				that._canExecuteChanged.Raise(args);
			}
		}
		void OnIsExecutingChanged(object? _, EventArgs args)
		{
			if (weakThis.TryGetTarget(out var that))
			{
				that.UpdateIsExecuting();
			}
		}
	}

	/// <inheritdoc />
	public bool IsExecuting { get; private set; }

	/// <inheritdoc />
	public bool CanExecute(object? parameter)
		=> _commands.Any(c => c.CanExecute(parameter));

	/// <inheritdoc />
	public void Execute(object? parameter)
	{
		foreach (var command in _commands.Where(c => c.CanExecute(parameter)))
		{
			command.Execute(parameter);
		}
	}

	private void UpdateIsExecuting()
	{
		var isExecuting = _commands.Any(c => c.IsExecuting);
		if (isExecuting != IsExecuting)
		{
			IsExecuting = isExecuting;

			_isExecutingChanged.Raise(EventArgs.Empty);
			_propertyChanged.Raise(_isExecutingChangedArgs);
		}
	}
}
