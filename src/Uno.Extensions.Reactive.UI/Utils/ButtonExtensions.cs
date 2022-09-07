using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Foundation;
using Uno.Extensions.Reactive.Commands;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// Extensions for <see cref="ButtonBase"/>.
/// </summary>
public static class ButtonExtensions
{
	#region IsExtendedVisualStatesEnabled (Attached DP)
	/// <summary>
	/// Backing property for the IsExtendedVisualStatesEnabled flag.
	/// </summary>
	public static readonly DependencyProperty IsExecutionTrackingEnabledProperty = DependencyProperty.RegisterAttached(
		"IsExecutionTrackingEnabled", typeof(bool), typeof(ButtonExtensions), new PropertyMetadata(default(bool), OnIsEnabledChanged));

	/// <summary>
	/// Gets a bool which indicates if the tracking of execution of AsyncCommand is enabled or not.
	/// This will enable the extended visual states on the Button: Idle, Executing, Failed, Succeed.
	/// It's also required to enable this to get the <see cref="LastExecutionErrorProperty"/> to be full-filled.
	/// </summary>
	/// <param name="button">The button to get the flag for.</param>
	/// <returns>True if async command execution tracking is enabled, false otherwise.</returns>
	public static bool GetIsExecutionTrackingEnabled(ButtonBase button)
		=> (bool)button.GetValue(IsExecutionTrackingEnabledProperty);

	/// <summary>
	/// Sets a bool which indicates if the tracking of execution of AsyncCommand is enabled or not.
	/// This will enable the extended visual states on the Button: Idle, Executing, Failed, Succeed.
	/// It's also required to enable this to get the <see cref="LastExecutionErrorProperty"/> to be updated.
	/// </summary>
	/// <param name="button">The button to set the flag for.</param>
	/// <param name="isEnabled">True to enable tracking of async command execution, false otherwise.</param>
	public static void SetIsExecutionTrackingEnabled(ButtonBase button, bool isEnabled)
		=> button.SetValue(IsExecutionTrackingEnabledProperty, isEnabled);
	#endregion

	#region ExtendedVisualStatesError (Attached DP)
	/// <summary>
	/// Backing property for the LastExecutionError.
	/// </summary>
	public static readonly DependencyProperty LastExecutionErrorProperty = DependencyProperty.RegisterAttached(
		"LastExecutionError", typeof(Exception), typeof(ButtonExtensions), new PropertyMetadata(default));

	/// <summary>
	/// Gets the exception raised by last execution of the command.
	/// The <see cref="IsExecutionTrackingEnabledProperty"/> as to be enabled to get this property updated.
	/// </summary>
	/// <param name="button">The button to get the error for.</param>
	/// <returns>The error raised by the command on last execution.</returns>
	public static Exception GetLastExecutionError(ButtonBase button)
		=> (Exception)button.GetValue(LastExecutionErrorProperty); 
	#endregion

	private static void OnIsEnabledChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
	{
		if (snd is not ButtonBase button)
		{
			return;
		}

		if (args.NewValue is true)
		{
			AsyncCommandExecutionTracker.GetOrCreate(button).Enable();
		}
		else if (AsyncCommandExecutionTracker.TryGet(button, out var states))
		{
			states!.Dispose();
		}
	}

	private class AsyncCommandExecutionTracker : IDisposable
	{
		private const string _idleStateName = "Idle";
		private const string _executingStateName = "Executing";
		private const string _failedStateName = "Failed";
		private const string _succeedStateName = "Succeed";

		private static readonly ConditionalWeakTable<ButtonBase, AsyncCommandExecutionTracker> _instances = new();

		public static AsyncCommandExecutionTracker GetOrCreate(ButtonBase button)
			=> _instances.GetValue(button, b => new AsyncCommandExecutionTracker(b));

		public static bool TryGet(ButtonBase button, /*CS0436 [NotNullWhen(true)]*/ out AsyncCommandExecutionTracker? instance)
			=> _instances.TryGetValue(button, out instance);

		private readonly HashSet<Guid> _activeExecutions = new();
		private readonly HashSet<Exception> _activeErrors = new();
		private readonly ButtonBase _button;
		
		private long? _commandRegistrationToken;
		private long? _isPressedRegistrationToken;
		private bool _isButtonPressed;
		private IAsyncAction? _toggleIsButtonPressed;
		private WeakReference<AsyncCommand>? _command;
		private bool _isDisposed;

		private AsyncCommandExecutionTracker(ButtonBase button)
		{
			_button = button;
		}

		public void Enable()
		{
			if (_isDisposed || _commandRegistrationToken is not null)
			{
				return;
			}

			_commandRegistrationToken = _button.RegisterPropertyChangedCallback(ButtonBase.CommandProperty, OnCommandChanged);
			_isPressedRegistrationToken = _button.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, OnIsPressedChanged);
			Subscribe(_button.Command);
		}

		private void OnIsPressedChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (sender is ButtonBase button && TryGet(button, out var that))
			{
				that!._toggleIsButtonPressed?.Cancel();
				if (button.IsPressed)
				{
					that._isButtonPressed = true;
				}
				else
				{
					_toggleIsButtonPressed = button.Dispatcher.RunIdleAsync(_ => that._isButtonPressed = false);
				}
			}
		}

		private static void OnCommandChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (sender is ButtonBase button && TryGet(button, out var that))
			{
				that!.Subscribe(button.Command);
			}
		}

		private void Subscribe(ICommand? command)
		{
			AsyncCommand? previous = default;
			_command?.TryGetTarget(out previous);

			if (_isDisposed || previous == command)
			{
				return; // Weird case !
			}

			if (previous is not null)
			{
				UnSubscribe(previous);
			}

			ClearState();

			if (command is AsyncCommand current)
			{
				_command = new WeakReference<AsyncCommand>(current);

				current.ExecutionStarted += OnExecutionStarted;
				current.ExecutionCompleted += OnExecutionCompleted;
			}
			else
			{
				_command = null;
			}
		}

		private void UnSubscribe(AsyncCommand command)
		{
			command.ExecutionStarted -= OnExecutionStarted;
			command.ExecutionCompleted -= OnExecutionCompleted;
		}

		private void OnExecutionStarted(object? sender, ExecutionStartedEventArgs e)
		{
			if (!_isDisposed && _isButtonPressed && e.Parameter == _button.CommandParameter)
			{
				if (_activeExecutions is { Count: 0 })
				{
					// On first active execution we make sure to clear the state of the previous executions

					_activeErrors.Clear();
					_button.SetValue(LastExecutionErrorProperty, null);
				}

				_activeExecutions.Add(e.Id);
				VisualStateManager.GoToState(_button, _executingStateName, useTransitions: true);
			}
		}

		private void OnExecutionCompleted(object? sender, ExecutionCompletedEventArgs e)
		{
			if (!_isDisposed && _activeExecutions.Remove(e.Id))
			{
				if (e.Error is not null)
				{
					_activeErrors.Add(e.Error);
				}

				if (_activeExecutions is { Count: 0 })
				{
					// At the end of the last active execution we leave the Executing state

					var (state, error) = _activeErrors switch
					{
						{ Count: 1 } => (_failedStateName, _activeErrors.First()),
						{ Count: > 1 } => (_failedStateName, new AggregateException(_activeErrors)),
						_ => (_succeedStateName, default)
					};
					_activeErrors.Clear();

					_button.SetValue(LastExecutionErrorProperty, error);
					VisualStateManager.GoToState(_button, state, useTransitions: true);
				}
			}
		}

		private void ClearState()
		{
			_toggleIsButtonPressed?.Cancel();
			_isButtonPressed = false;
			_activeExecutions.Clear();
			_activeErrors.Clear();
			_button.SetValue(LastExecutionErrorProperty, null);
			VisualStateManager.GoToState(_button, _idleStateName, useTransitions: false);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_isDisposed = true;

			if (_commandRegistrationToken is not null)
			{
				_button.UnregisterPropertyChangedCallback(ButtonBase.CommandProperty, _commandRegistrationToken.Value);
			}

			if (_isPressedRegistrationToken is not null)
			{
				_button.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, _isPressedRegistrationToken.Value);
			}

			if (_command?.TryGetTarget(out var command) ?? false)
			{
				UnSubscribe(command);
			}

			ClearState();
		}
	}
}
