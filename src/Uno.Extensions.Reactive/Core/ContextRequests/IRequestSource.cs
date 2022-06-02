using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal interface IRequestSource : IAsyncEnumerable<IContextRequest>, IDisposable
{
	public void Send(IContextRequest request);
}
