using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.Extensions.Reactive;

public static class Command
{
	internal static Action<Exception> _defaultErrorHandler = e => typeof(AsyncCommand).Log().Error("Failed execute command.", e);

	public static IAsyncCommand Async(ActionAsync execute, [CallerMemberName] string? name = null)
	{
		if (execute.Target is null)
		{
			throw new InvalidOperationException("The delegate provided in the Command.Async must not be a static method.");
		}

		return AttachedProperty.GetOrCreate(
			execute.Target,
			execute,
			name,
			(o, e, n) => new AsyncCommand(n, new CommandConfig{Execute = (_, ct) => e(ct)}, _defaultErrorHandler, SourceContext.GetOrCreate(o)));
	}
}
