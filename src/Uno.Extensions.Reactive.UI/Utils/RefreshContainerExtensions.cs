using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

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

	private static void OnCommandChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
	{
		if (snd is not RefreshContainer container)
		{
			return;
		}

		container.RefreshRequested -= OnRefreshRequested;

		if (args.NewValue is ICommand)
		{
			container.RefreshRequested += OnRefreshRequested;
		}
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
#pragma warning disable Uno0001 // Uno not up to date
			var deferral = args.GetDeferral();
#pragma warning restore Uno0001
			asyncCommand.PropertyChanged += OnPropertyChanged;

			void OnPropertyChanged(object? _, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == nameof(IAsyncCommand.IsExecuting)
					&& !asyncCommand.IsExecuting)
				{
					asyncCommand.PropertyChanged -= OnPropertyChanged;
					deferral.Complete();
				}
			}
		}
	}
}
