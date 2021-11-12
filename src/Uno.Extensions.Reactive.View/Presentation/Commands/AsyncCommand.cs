using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.System;
using Uno.Extensions.Reactive.View.Utils;

namespace Uno.Extensions.Reactive;

internal sealed class AsyncCommand : IAsyncCommand, IDisposable
{
	private static readonly object _null = new();

	private readonly CancellationTokenSource _ct = new();
	private readonly Dictionary<object, int> _executions = new();

	private readonly string? _name;
	private readonly Action<Exception> _errorHandler;
	private readonly SourceContext _context;
	private readonly DispatcherQueue _dispatcher;

	private readonly ICollection<SubCommand> _children;

	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged;

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	public AsyncCommand(
		string? name,
		CommandConfig config,
		Action<Exception> errorHandler,
		SourceContext context,
		DispatcherQueue? dispatcher = null)
	{
		_name = name;
		_children = new List<SubCommand>(1){new (config, this)};
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_dispatcher = DispatcherHelper.GetDispatcher(dispatcher);

		SubscribeToExternalParameters();
	}

	public AsyncCommand(
		string? name,
		IEnumerable<CommandConfig> configs,
		Action<Exception> errorHandler,
		SourceContext context,
		DispatcherQueue? dispatcher = null)
	{
		_name = name;
		_children = configs.Select(config => new SubCommand(config, this)).ToList();
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_dispatcher = DispatcherHelper.GetDispatcher(dispatcher);

		SubscribeToExternalParameters();
	}

	/// <inheritdoc />
	public bool IsExecuting { get; private set; }

	/// <inheritdoc />
	public bool CanExecute(object? parameter)
		=> _children.Any(child => child.CanExecute(parameter));

	/// <inheritdoc />
	public void Execute(object? parameter)
	{
		if (_children.Aggregate(false, (isExecuting, child) => isExecuting | child.TryExecute(parameter, _context, _ct.Token)))
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

	private bool IsExecutingFor(object? parameter)
	{
		lock (_executions)
		{
			return _executions.ContainsKey(parameter ?? _null);
		}
	}

	private void ReportExecutionStarting(object? parameter)
	{
		parameter ??= _null;

		// Note: We DO NOT UpdateExecutionState: it will be done only once by the Execute.
		lock (_executions)
		{
			if (!_executions.TryGetValue(parameter, out var count))
			{
				count = 0;
			}

			_executions[parameter] = count + 1;
		}
	}

	private void ReportExecutionEnded(object? parameter)
	{
		parameter ??= _null;

		var needsUiUpdate = false;
		lock (_executions)
		{
			if (_executions.TryGetValue(parameter, out var count))
			{
				if (--count <= 0)
				{
					needsUiUpdate = _executions.Remove(parameter);
				}
				else
				{
					_executions[parameter] = count;
				}
			}
		}

		if (needsUiUpdate)
		{
			UpdateIsExecuting();
		}
	}

	private void SubscribeToExternalParameters()
	{
		if (_dispatcher.HasThreadAccess)
		{
			foreach (var child in _children)
			{
				_ = child.SubscribeToParameter(_context, _ct.Token);
			}
		}
		else if (_children.Any())
		{
			_dispatcher.TryEnqueue(SubscribeToExternalParameters);
		}
	}

	private void UpdateCanExecute()
	{
		if (_dispatcher.HasThreadAccess)
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			_dispatcher.TryEnqueue(UpdateCanExecute);
		}
	}

	private void UpdateIsExecuting()
	{
		if (_dispatcher.HasThreadAccess)
		{
			lock (_executions)
			{
				IsExecuting = _executions.Count > 0;
			}

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExecuting)));
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			_dispatcher.TryEnqueue(UpdateIsExecuting);
		}
	}

	private sealed class SubCommand
	{
		private readonly CommandConfig _config;
		private readonly AsyncCommand _command;

		private (object? value, bool isValid)? _externalParameter;

		public SubCommand(CommandConfig config, AsyncCommand command)
		{
			_config = config;
			_command = command;
		}

		public async Task SubscribeToParameter(SourceContext context, CancellationToken ct)
		{
			if (_config.Parameter is null)
			{
				return;
			}

			_externalParameter = (null, false);
			_command.UpdateCanExecute();

			var parameters = _config
				.Parameter(context)
				.Where(message => message.Changes.Contains(MessageAxis.Data))
				.WithCancellation(ct)
				.ConfigureAwait(true);
			await foreach (var parameter in parameters)
			{
				bool isValid, wasValid = _externalParameter is { isValid: true };
				try
				{
					isValid = parameter.Current.Data.IsSome(out var value)
						&& (_config.CanExecute?.Invoke(value) ?? true);

					_externalParameter = (value, isValid);
				}
				catch (Exception error)
				{
					_command.ReportError(error, when: "validating can execute of external parameter");
					_externalParameter = (null, isValid = false);
				}

				if (wasValid != isValid)
				{
					_command.UpdateCanExecute();
				}
			}
		}

		public bool CanExecute(object? parameter)
		{
			if (_externalParameter is { } externalParameter)
			{
				if (!externalParameter.isValid)
				{
					return false;
				}

				parameter = externalParameter.value;
			}

			return !_command.IsExecutingFor(parameter) && (_config.CanExecute?.Invoke(parameter) ?? true);
		}

		public bool TryExecute(object? parameter, SourceContext context, CancellationToken ct)
		{
			if (_externalParameter is { } externalParameter)
			{
				if (!externalParameter.isValid)
				{
					return false;
				}

				parameter = externalParameter.value;
			}

			if (!(_config.CanExecute?.Invoke(parameter)?? true))
			{
				return false;
			}

			_command.ReportExecutionStarting(parameter);

			Task.Run(
					async () =>
					{
						try
						{
							using var _ = _command._context.AsCurrent();
							await _config.Execute(parameter, _command._ct.Token);
						}
						catch (Exception error)
						{
							_command.ReportError(error, when: $"executing command with '{parameter ?? "-null-"}'");
						}
					},
					_command._ct.Token)
				.ContinueWith((_, state) =>
					{
						try
						{
							var (command, arg) = ((AsyncCommand, object?))state;
							command.ReportExecutionEnded(arg);
						}
						catch (Exception) { } // Almost impossible, but an error here would crash the app
					},
					(_command, parameter),
					TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);

			return true;
		}
	}

	/// <inheritdoc />
	public void Dispose()
		=> _ct.Cancel();
}
