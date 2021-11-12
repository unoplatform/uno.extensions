using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive;

public interface ISignal<out T>
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	IAsyncEnumerable<T> GetSource(SourceContext context, CancellationToken ct = default);
}
