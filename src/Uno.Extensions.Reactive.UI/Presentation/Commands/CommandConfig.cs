using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

internal record struct CommandConfig
{
	public Func<SourceContext, IAsyncEnumerable<IMessage>>? Parameter { get; set; }

	public Predicate<object?>? CanExecute { get; set; }

	public AsyncAction<object?> Execute { get; set; }
}
