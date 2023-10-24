using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive.Commands;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create an <see cref="IAsyncCommand"/>.
/// </summary>
public static class Command
{
	/// <summary>
	/// Defines the default error handler that is being invoked if an exception is raised by a command.
	/// </summary>
	/// <remarks>By default, this will log the error.</remarks>
	/// <remarks>This is designed to be a "last chance" handler, you should handle exceptions in each command.</remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static Action<Exception> DefaultErrorHandler { get; set; } = e => LogExtensions.Log<AsyncCommand>().Error(e, "Failed to execute command.");

	/// <summary>
	/// Creates a command from an async method
	/// </summary>
	/// <param name="execute">The action to execute.</param>
	/// <param name="name">The name of the command.</param>
	/// <returns>The command.</returns>
	/// <exception cref="InvalidOperationException">The execute delegate is static.</exception>
	public static IAsyncCommand Async(AsyncAction execute, [CallerMemberName] string? name = null)
	{
		if (execute.Target is null)
		{
			throw new InvalidOperationException("The delegate provided in the Command.Async must not be a static method.");
		}

		return AttachedProperty.GetOrCreate(
			execute.Target,
			execute,
			name,
			(o, e, n) => new AsyncCommand(n, new CommandConfig{Execute = (_, ct) => e(ct)}, DefaultErrorHandler, SourceContext.GetOrCreate(o)));
	}

	/// <summary>
	/// Creates a command
	/// </summary>
	/// <param name="build">The delegate to build the command.</param>
	/// <param name="name">The name of the command.</param>
	/// <returns>The command.</returns>
	/// <exception cref="InvalidOperationException">The execute delegate is static.</exception>
	public static IAsyncCommand Create(Action<ICommandBuilder> build, [CallerMemberName] string? name = null)
	{
		if (build.Target is null)
		{
			throw new InvalidOperationException("The delegate provided in the Command.Create must not be a static method.");
		}

		return AttachedProperty.GetOrCreate(
			build.Target,
			build,
			name,
			(o, b, n) =>
			{
				var ctx = SourceContext.GetOrCreate(o);
				var builder = new CommandBuilder<object>(name!);
				build(builder);
				return builder.Build(ctx, DefaultErrorHandler);
			});
	}

	/// <summary>
	/// Creates a command
	/// </summary>
	/// <param name="build">The delegate to build the command.</param>
	/// <param name="name">The name of the command.</param>
	/// <returns>The command.</returns>
	/// <exception cref="InvalidOperationException">The execute delegate is static.</exception>
	public static IAsyncCommand Create<T>(Action<ICommandBuilder<T>> build, [CallerMemberName] string? name = null)
	{
		if (build.Target is null)
		{
			throw new InvalidOperationException("The delegate provided in the Command.Create must not be a static method.");
		}

		return AttachedProperty.GetOrCreate(
			build.Target,
			build,
			name,
			(o, b, n) =>
			{
				var ctx = SourceContext.GetOrCreate(o);
				var builder = new CommandBuilder<T>(name!);
				build(builder);
				return builder.Build(ctx, DefaultErrorHandler);
			});
	}
}
