using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Events;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// An <see cref="ICommand"/> that exposes it's executing state.
/// </summary>
/// <remarks>
/// This command supports parallel executions with different command parameters.
/// In that case the <see cref="IsExecuting"/> will remain false.
/// </remarks>
/// <remarks>Yous should not use this type directly. Instead use builder provided by <see cref="Command"/>, or use code gen.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class AsyncCommand : IAsyncCommand, IDisposable
{
	private static readonly object _null = new();
	private static readonly PropertyChangedEventArgs _isExecutingChangedArgs = new(nameof(IsExecuting));

	private readonly CancellationTokenSource _ct = new();
	private readonly Dictionary<object, int> _executions = new();

	private readonly string? _name;
	private readonly Action<Exception> _errorHandler;
	private readonly SourceContext _context;

	private readonly ICollection<SubCommand> _children;
	private readonly LazyDispatcherProvider _dispatcher;
	private readonly EventManager<EventHandler, EventArgs> _canExecuteChanged;
	private readonly EventManager<EventHandler, EventArgs> _isExecutingChanged;
	private readonly EventManager<PropertyChangedEventHandler, PropertyChangedEventArgs> _propertyChanged;
	private readonly EventManager<ExecutionCompletedEventArgs> _executionCompleted;

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
	/// Event raised when the command is being executed.
	/// </summary>
	public event EventHandler<ExecutionStartedEventArgs> ExecutionStarted;

	/// <summary>
	/// Event raised when the command completes an async execution.
	/// </summary>
	public event EventHandler<ExecutionCompletedEventArgs> ExecutionCompleted
	{
		add => _executionCompleted.Add(value);
		remove => _executionCompleted.Remove(value);
	}

#pragma warning disable CS8618 // This is a private base ctor which is invoked only by all other public ctors which are initializing missing fields.
	private AsyncCommand()
#pragma warning restore CS8618
	{
		_dispatcher = new(onFirstResolved: SubscribeToExternalParameters);
		_canExecuteChanged = new(this, h => h.Invoke, isCoalescable: true, schedulersProvider: _dispatcher.FindDispatcher);
		_isExecutingChanged = new(this, h => h.Invoke, isCoalescable: true, schedulersProvider: _dispatcher.FindDispatcher);
		_propertyChanged = new(this, h => h.Invoke, isCoalescable: false, schedulersProvider: _dispatcher.FindDispatcher);
		_executionCompleted = new(this, isCoalescable: false, schedulersProvider: _dispatcher.FindDispatcher);
	}

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="name">The name of teh command, used for debug and log purposes.</param>
	/// <param name="config">The configuration of the command.</param>
	/// <param name="errorHandler">The last chance error handler.</param>
	/// <param name="context">The context to which this command belongs.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="errorHandler"/> or <paramref name="context"/> is null.</exception>
	public AsyncCommand(
		string? name,
		CommandConfig config,
		Action<Exception> errorHandler,
		SourceContext context)
		: this()
	{
		_name = name;
		_children = new List<SubCommand>(1) { new(config, this) };
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		_context = context ?? throw new ArgumentNullException(nameof(context));

		_dispatcher.TryRunCallback();
	}

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="name">The name of teh command, used for debug and log purposes.</param>
	/// <param name="configs">The configurations of the command.</param>
	/// <param name="errorHandler">The last chance error handler.</param>
	/// <param name="context">The context to which this command belongs.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="errorHandler"/> or <paramref name="context"/> is null.</exception>
	public AsyncCommand(
		string? name,
		IEnumerable<CommandConfig> configs,
		Action<Exception> errorHandler,
		SourceContext context)
		: this()
	{
		_name = name;
		_children = configs.Select(config => new SubCommand(config, this)).ToList();
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		_context = context ?? throw new ArgumentNullException(nameof(context));

		_dispatcher.TryRunCallback();
	}

	/// <inheritdoc />
	public bool IsExecuting { get; private set; }

	/// <inheritdoc />
	public bool CanExecute(object? parameter)
	{
		// No matter if we are on the UI thread or not, we must make sure that the command is fully initialized.
		_dispatcher.RunCallback();

		return _children.Any(child => child.CanExecute(parameter));
	}

	/// <inheritdoc />
	public void Execute(object? parameter)
	{
		// No matter if we are on the UI thread or not, we must make sure that the command is fully initialized.
		_dispatcher.RunCallback();

		if (_children.Aggregate(false, (isExecuting, child) => isExecuting | child.TryExecute(parameter, _context, _ct.Token)))
		{
			UpdateIsExecuting();
		}
	}

	private void UpdateCanExecute()
	{
		_canExecuteChanged.Raise(EventArgs.Empty);
	}

	private void UpdateIsExecuting()
	{
		(bool @is, bool was) executing;
		lock (_executions)
		{
			executing = (_executions.Count > 0, IsExecuting);
			IsExecuting = executing.@is;
		}

		if (executing.@is != executing.was)
		{
			_isExecutingChanged.Raise(EventArgs.Empty);
			_propertyChanged.Raise(_isExecutingChangedArgs);
		}

		_canExecuteChanged.Raise(EventArgs.Empty);
	}

	private bool IsExecutingFor(object? coercedParameter)
	{
		lock (_executions)
		{
			return _executions.ContainsKey(coercedParameter ?? _null);
		}
	}

	private void ReportExecutionStarting(Guid executionId, object? coercedParameter, object? viewParameter)
	{
		coercedParameter ??= _null;

		// Note: We DO NOT UpdateIsExecuting: it will be done only once by the Execute.
		lock (_executions)
		{
			if (!_executions.TryGetValue(coercedParameter, out var count))
			{
				count = 0;
			}

			_executions[coercedParameter] = count + 1;
		}

		ExecutionStarted?.Invoke(this, new ExecutionStartedEventArgs(executionId, viewParameter));
	}

	private void ReportExecutionEnded(Guid executionId, object? coercedParameter, object? viewParameter, Exception? error)
	{
		if (error is not null)
		{
			ReportError(error, when: $"executing command with '{coercedParameter ?? "-null-"}'");
		}

		coercedParameter ??= _null;

		var needsUiUpdate = false;
		lock (_executions)
		{
			if (_executions.TryGetValue(coercedParameter, out var count))
			{
				if (--count <= 0)
				{
					needsUiUpdate = _executions.Remove(coercedParameter);
				}
				else
				{
					_executions[coercedParameter] = count;
				}
			}
		}

		_executionCompleted.Raise(new ExecutionCompletedEventArgs(executionId, viewParameter, error));

		if (needsUiUpdate)
		{
			UpdateIsExecuting();
		}
	}

	private void ReportError(Exception error, string when)
	{
		try
		{
			_errorHandler(new InvalidOperationException($"Command '{_name}' failed when {when}.", error));
		}
		catch { }
	}

	private async void SubscribeToExternalParameters()
	{
		foreach (var child in _children)
		{
			_ = child.SubscribeToParameter(_context, _ct.Token);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_ct.Cancel();
		_canExecuteChanged.Dispose();
		_isExecutingChanged.Dispose();
		_propertyChanged.Dispose();
		_executionCompleted.Dispose();
	}
}
