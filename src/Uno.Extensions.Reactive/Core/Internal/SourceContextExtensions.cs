using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal static class SourceContextExtensions
{
	public static IDispatcher? FindDispatcher(this SourceContext context)
		=> context.Owner.Dispatcher ?? context.Parent?.FindDispatcher();
}
