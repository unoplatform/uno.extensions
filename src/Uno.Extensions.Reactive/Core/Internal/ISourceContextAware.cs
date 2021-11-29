using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A tag interface that indicates that the owner of <see cref="SourceContext"/> will take care to dispose that context.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface ISourceContextAware
{
}
