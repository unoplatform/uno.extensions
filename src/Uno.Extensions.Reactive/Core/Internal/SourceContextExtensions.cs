using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Set of extensions methods on <see cref="SourceContext"/>.
/// </summary>
public static class SourceContextExtensions
{
	/// <summary>
	/// Tries to retrieve the UI thread of the subscriber that has created this source context.
	/// </summary>
	/// <param name="context">The source context from which the dispatcher should be resolved</param>
	/// <returns>The dispatcher associated to UI thread of the subscriber if any.</returns>
	public static IDispatcher? FindDispatcher(this SourceContext context)
		=> context.Owner.Dispatcher ?? context.Parent?.FindDispatcher();
}
