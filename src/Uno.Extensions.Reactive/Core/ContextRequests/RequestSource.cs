using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class RequestSource : IRequestSource
{
	/// <inheritdoc />
	public event EventHandler<IContextRequest>? RequestRaised;

	/// <inheritdoc />
	public void Send(IContextRequest request)
		=> RequestRaised?.Invoke(this, request);

	/// <inheritdoc />
	public void Dispose()
		=> RequestRaised?.Invoke(this, End.Instance);
}
