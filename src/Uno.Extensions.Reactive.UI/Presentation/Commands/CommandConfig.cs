using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The raw configuration of a command.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)] // This should be used only by code gen
public record struct CommandConfig
{
	/// <summary>
	/// Command parameter provider (if the parameter is not coming from the view).
	/// </summary>
	public Func<SourceContext, IAsyncEnumerable<IMessage>>? Parameter { get; set; }

	/// <summary>
	/// Can execute delegate.
	/// </summary>
	public Predicate<object?>? CanExecute { get; set; }

	/// <summary>
	/// The action to execute.
	/// </summary>
	public AsyncAction<object?> Execute { get; set; }
}
