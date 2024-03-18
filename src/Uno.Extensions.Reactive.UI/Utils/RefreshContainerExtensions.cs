#if !WINUI
#pragma warning disable CS0618 // RefreshContainer is flagged as obsolete UWP version of uno as it seems to not be supported also by windows
#endif

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Foundation;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// Extensions for the <see cref="RefreshContainer"/>.
/// </summary>
public static class RefreshContainerExtensions
{
	#region Command (Attached DP)
	/// <summary>
	/// The backing property for the Command
	/// </summary>
	public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
		"Command", typeof(ICommand), typeof(RefreshContainerExtensions), new PropertyMetadata(default(ICommand), OnCommandChanged));

	/// <summary>
	/// Gets the command attached on a <paramref name="container" />
	/// </summary>
	/// <param name="container">The refresh container to get the command for.</param>
	/// <returns>The attached command, if any.</returns>
	public static ICommand? GetCommand(RefreshContainer container)
		=> (ICommand?)container.GetValue(CommandProperty);

	/// <summary>
	/// Attach the <paramref name="command"/> on the given <paramref name="container"/>.
	/// </summary>
	/// <param name="container">The refresh container on which command should be set.</param>
	/// <param name="command">The command to set.</param>
	public static void SetCommand(RefreshContainer container, ICommand? command)
		=> container.SetValue(CommandProperty, command);
	#endregion

	#region CommandParameter (Attached DP)
	/// <summary>
	/// The backing property for the CommandParameter
	/// </summary>
	public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
		"CommandParameter", typeof(object), typeof(RefreshContainerExtensions), new PropertyMetadata(default(object)));

	/// <summary>
	/// Gets the command parameter attached on a <paramref name="container" />
	/// </summary>
	/// <param name="container">The refresh container to get the command parameter for.</param>
	/// <returns>The attached command parameter, if any.</returns>
	public static object? GetCommandParameter(RefreshContainer container)
		=> container.GetValue(CommandParameterProperty);

	/// <summary>
	/// Attach the <paramref name="parameter"/> on the given <paramref name="container"/>.
	/// </summary>
	/// <param name="container">The refresh container on which command should be set.</param>
	/// <param name="parameter">The command parameter to set.</param>
	public static void SetCommandParameter(RefreshContainer container, object? parameter)
		=> container.SetValue(CommandParameterProperty, parameter);
	#endregion

	private static readonly ConditionalWeakTable<RefreshContainer, Deferral> _deferrals = new();

	private static void OnCommandChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
	{
		if (snd is not RefreshContainer container)
		{
			return;
		}

		CompleteDeferral(container);

#if WINUI
		container.RefreshRequested -= OnRefreshRequested;

		if (args.NewValue is ICommand)
		{
			container.RefreshRequested += OnRefreshRequested;
		}
#endif
	}

	private static void OnRefreshRequested(RefreshContainer snd, RefreshRequestedEventArgs args)
	{
		var command = GetCommand(snd);
		var parameter = GetCommandParameter(snd);

		if (!(command?.CanExecute(parameter) ?? false))
		{
			return;
		}

		command.Execute(parameter);

		if (command is IAsyncCommand { IsExecuting: true } asyncCommand)
		{
			CompleteDeferral(snd); // Safety: We should not be invoked if there is already a pending deferral!

#pragma warning disable Uno0001 // Uno not up to date
			var deferral = args.GetDeferral();
#pragma warning restore Uno0001
			var weakContainer = new WeakReference<RefreshContainer>(snd);
			_deferrals.Add(snd, deferral);

			asyncCommand.PropertyChanged += OnPropertyChanged;

			void OnPropertyChanged(object? _, PropertyChangedEventArgs args)
			{
				if (!weakContainer.TryGetTarget(out var container))
				{
					// The container has been collected, nothing to do anymore!
					asyncCommand.PropertyChanged -= OnPropertyChanged;

					return;
				}

				if (args.PropertyName == nameof(IAsyncCommand.IsExecuting)
					&& !asyncCommand.IsExecuting)
				{
					asyncCommand.PropertyChanged -= OnPropertyChanged;
					CompleteDeferral(container);
				}
			}
		}
	}

	private static void CompleteDeferral(RefreshContainer container)
	{
		if (_deferrals.TryGetValue(container, out var deferral))
		{
			deferral.Complete();
			_deferrals.Remove(container);
		}
	}
}
