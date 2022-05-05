using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal interface IStateImpl
{
	SourceContext Context { get; }
}
